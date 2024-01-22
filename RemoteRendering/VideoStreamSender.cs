using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen.Abstractions;
using Godot.WebRTC;


namespace Godot.RemoteRendering
{
    
    public class SendManager
    {
        private ulong sampleTime_us = 0;
        private readonly ulong sampleDuration_us;

        private HashSet<int> _openedTrackSet = new ();

        public HashSet<int> TrackSet => _openedTrackSet;

        public SendManager(int fps)
        {
            sampleDuration_us = 1000 * 1000 / (ulong)fps;
            sampleTime_us = ulong.MaxValue - sampleDuration_us + 1;
        }

        public void OpenTrack(int id)
        {
            _openedTrackSet.Add(id);
        }

        public void CloseTrack(int id)
        {
            _openedTrackSet.Remove(id);
        }

        public void NextSend()
        {
            sampleTime_us += sampleDuration_us;
        }
        
        public ulong GetSampleTime()
        {
            return sampleTime_us;
        }

        public ulong GetSampleDuration()
        {
            return sampleDuration_us;
        }
        
    }

    public partial class VideoStreamSender : StreamSender
    {
        private Dictionary<int, Dictionary<int, Track>> _mapPeerIdAndTrack = new ();
        private SendManager _sendManager;
        
        private Thread _sendThread;
        private CancellationTokenSource _cancellationTokenSource;

        private H264Encoder _encoder;
        private VideoFrameSource _frameSource;
        private int fps;

        private ulong _startTime;
        private uint _startTimestamp = 10000;
        private bool _isRunning = false;
        public VideoStreamSender(int fps)
        {
            this.fps = fps;
            _sendManager = new SendManager(fps);
        }
        
        public override void _EnterTree()
        {
            _frameSource = new VideoFrameSource();
            GD.Print("EnterTree1");
            AddChild(_frameSource);
        }

        public void AddVideo(PeerConnection pc, RtcTrackInit init)
        {
            Track track = pc.AddTrackEx(init);
            int tr = track.Id;
            
            SetPacketizationHandler(tr, init);
            if (_mapPeerIdAndTrack.ContainsKey(pc.Id))
            {
                _mapPeerIdAndTrack.TryGetValue(pc.Id, out Dictionary<int, Track>tDic);
                tDic.Add(tr, track);
            }
            else
            {
                Dictionary<int, Track> tDic = new Dictionary<int, Track>
                {
                    { tr, track }
                };
                _mapPeerIdAndTrack.Add(pc.Id, tDic);
            }
            track.Opened += OnOpen;
            track.Closed += OnClose;
            
            GD.Print("VideoStreamSender Add Track OK! with startTimestamp = " + _startTimestamp);
        }

        public void SetPacketizationHandler(int tr, RtcTrackInit init)
        {
            RtcPacketizationHandlerInit packetizationHandlerInit = new RtcPacketizationHandlerInit
            {
                ssrc = init.ssrc,
                cname = init.name,
                payloadType = (byte)init.payloadType,
                nalUnitSeparator = RtcNalUnitSeparator.RTC_NAL_SEPARATOR_START_SEQUENCE,
                sequenceNumber = (ushort)_startTimestamp,
                timestamp = _startTimestamp
            };
            switch(init.codec) {
                case RtcCodec.RTC_CODEC_H264:
                {
                    packetizationHandlerInit.clockRate = 90 * 1000;
                    if (NativeMethods.rtcSetH264PacketizationHandler(tr, ref packetizationHandlerInit) < 0)
                    {
                        GD.PrintErr("VideoStreamSender: Track SetPacketizationHandler Error1!");
                    }
                    break;
                }
                case RtcCodec.RTC_CODEC_H265:
                {
                    break;
                }
                case RtcCodec.RTC_CODEC_VP8:
                {
                    break;
                }
                case RtcCodec.RTC_CODEC_VP9:
                {
                    break;
                }
                case RtcCodec.RTC_CODEC_AV1:
                {
                    break;
                }
                default:
                {
                    GD.PrintErr("RtcCodec Error!");
                    break;
                }
            }
            if (NativeMethods.rtcChainRtcpSrReporter(tr) < 0)
            {
                GD.PrintErr("VideoStreamSender: Track rtcChainRtcpSrReporter Error!");
            }
            if (NativeMethods.rtcChainRtcpNackResponder(tr, 512) < 0)
            {
                GD.PrintErr("VideoStreamSender: Track rtcChainRtcpNackResponder Error!");
            }
        }

        public void OnOpen(int id)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _sendManager.OpenTrack(id);
                GD.PrintRich($"[color=red]Track {id} Opened![/color]");
                _encoder = new H264Encoder(
                    _frameSource.GetImageWidth(),
                    _frameSource.GetImageHeight(),
                    AVPixelFormat.AV_PIX_FMT_RGB24,
                    fps
                );
                _cancellationTokenSource = new CancellationTokenSource();
                _sendThread = new Thread(() => Send(_cancellationTokenSource.Token));
                _sendThread.Start();
            }
            else
            {
                _sendManager.OpenTrack(id);
                GD.PrintRich($"[color=red]Track {id} Opened![/color]");
            }
        }

        public void OnClose(int id)
        {
            _sendManager.CloseTrack(id);
            GD.PrintRich($"[color=red]Track {id} Closed![/color]");
            if (_sendManager.TrackSet.Count == 0)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    if (!_sendThread.Join(1000))
                    {
                        // Thread didn't terminate in 1 second, consider other ways to handle it
                    }
                    _sendThread = null;
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                    _isRunning = false;
                }
                
            }
        }

        private void Send(CancellationToken cancellationToken)
        {
            _startTime = PluginUtils.CurrentTimeInMicroseconds();
            ulong numOfEmptyFrame = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                _sendManager.NextSend();
                ulong sampleTime = _sendManager.GetSampleTime();
                
                IntPtr dataPtr = IntPtr.Zero;
                int dataSize = _encoder.EncodeFrame(_frameSource.GetFrameData(), (long)sampleTime, ref dataPtr);
                if (dataSize > 0)
                {
                    ulong currentTime = PluginUtils.CurrentTimeInMicroseconds();
                    ulong elapsed = currentTime - _startTime;
                    ulong realSampleTime = sampleTime - numOfEmptyFrame * _sendManager.GetSampleDuration();
                    
                    if (realSampleTime > elapsed)
                    {
                        //GD.PrintRich($"[color=green]{realSampleTime - elapsed}[/color]");
                        Task.Delay((int)((realSampleTime - elapsed) / 1000)).Wait();
                    }

                    foreach (var id in _sendManager.TrackSet)
                    {
                        SendEncodedData(id, sampleTime, dataPtr, dataSize);
                    }
                    
                }
                else
                {
                    numOfEmptyFrame += 1;
                }
                Marshal.FreeHGlobal(dataPtr);
            }
        }

        private void SendEncodedData(int id, ulong sampleTime, IntPtr dataPtr, int size)
        {
            double elapsedSeconds = (double)sampleTime / (1000 * 1000);
            uint elapsedTimestamp = 0;
            
            if (NativeMethods.rtcTransformSecondsToTimestamp(id, elapsedSeconds, ref elapsedTimestamp) < 0)
            {
                GD.Print("------------rtcTransformSecondsToTimestamp Error!");
            }
            
            if (NativeMethods.rtcSetTrackRtpTimestamp(id, _startTimestamp + elapsedTimestamp) < 0)
            {
                GD.Print("------------rtcSetTrackRtpTimestamp Error!");
            }
            
            uint lastReportTimestamp = 0;
            NativeMethods.rtcGetLastTrackSenderReportTimestamp(id, ref lastReportTimestamp);

            uint reportElapsedTimestamp = _startTimestamp + elapsedTimestamp - lastReportTimestamp;
            double reportElapsed = 0;
            if (NativeMethods.rtcTransformTimestampToSeconds(id, reportElapsedTimestamp, ref reportElapsed) < 0)
            {
                GD.Print("------------rtcTransformTimestampToSeconds Error!");
            }
            
            if (reportElapsed > 1)
            {
                NativeMethods.rtcSetNeedsToSendRtcpSr(id);
            }
            //GD.PrintRich("[color=red]Sending![/color]");
            if (NativeMethods.rtcSendMessage(id, dataPtr, size) < 0)
            {
                GD.Print("------------rtcSendMessage Error!");
            }
        }

    }

}