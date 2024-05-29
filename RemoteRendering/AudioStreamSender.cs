using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Godot.WebRTC;

namespace Godot.RemoteRendering
{
    public partial class AudioStreamSender : StreamSender
    {
        private Dictionary<int, Dictionary<int, Track>> _mapPeerIdAndTrack = new ();
        private SendManager _sendManager = new(50);
        
        private Thread _sendThread;
        private CancellationTokenSource _cancellationTokenSource;

        private AudioEncoder _encoder;
        private AudioFrameSource _frameSource;
        private int _mixRate = 48000;
        
        private ulong _startTime;
        private uint _startTimestamp = 10000;
        private bool _isRunning = false;

        public AudioStreamSender()
        {
            
        }

        public override void _EnterTree()
        {
            _frameSource = new AudioFrameSource();
            AddChild(_frameSource);
        }

        public void AddAudio(PeerConnection pc, RtcTrackInit init)
        {
            Track track = pc.AddTrackEx(init);
            int tr = track.Id;
            
            SetPacketizationHandler(tr, init);
            if (_mapPeerIdAndTrack.ContainsKey(pc.Id))
            {
                _mapPeerIdAndTrack[pc.Id].Add(tr, track);
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
            
            GD.Print("AudioStreamSender Add Track OK! with startTimestamp = " + _startTimestamp);
        }
        
        public void SetPacketizationHandler(int tr, RtcTrackInit init)
        {
            RtcPacketizationHandlerInit packetizationHandlerInit = new RtcPacketizationHandlerInit
            {
                ssrc = init.ssrc,
                cname = init.name,
                payloadType = (byte)init.payloadType,
                nalSeparator = RtcNalUnitSeparator.RTC_NAL_SEPARATOR_START_SEQUENCE,
                sequenceNumber = (ushort)_startTimestamp,
                timestamp = _startTimestamp
            };
            switch(init.codec) {
                case RtcCodec.RTC_CODEC_OPUS:
                {
                    packetizationHandlerInit.clockRate = (uint)_mixRate;
                    if (NativeMethods.rtcSetOpusPacketizationHandler(tr, ref packetizationHandlerInit) < 0)
                    {
                        GD.PrintErr("AudioStreamSender: Track SetPacketizationHandler Error!");
                    }
                    if (_encoder == null) _encoder = new OpusEncoder();
                    break;
                }
                case RtcCodec.RTC_CODEC_AAC:
                {
                    break;
                }
                case RtcCodec.RTC_CODEC_PCMA:
                {
                    break;
                }
                case RtcCodec.RTC_CODEC_PCMU:
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
                GD.PrintErr("AudioStreamSender: Track rtcChainRtcpSrReporter Error!");
            }
            if (NativeMethods.rtcChainRtcpNackResponder(tr, 512) < 0)
            {
                GD.PrintErr("AudioStreamSender: Track rtcChainRtcpNackResponder Error!");
            }
        }

        public void OnOpen(int id)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _sendManager.OpenTrack(id);
                GD.PrintRich($"[color=red]Track {id} Opened![/color]");
                _encoder.Initialize(_frameSource.GetMixRate(), _frameSource.GetChannels());
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
            _frameSource.ClearBuffer();
            _startTime = PluginUtils.CurrentTimeInMicroseconds();
            while (!cancellationToken.IsCancellationRequested)
            {
                IntPtr dataPtr = IntPtr.Zero;
                int dataSize = _encoder.EncodeFrame(_frameSource.GetAudioFrames(960), ref dataPtr);
                if (dataSize > 0)
                {
                    _sendManager.NextSend();
                    ulong sampleTime = _sendManager.GetSampleTime();
                    ulong currentTime = PluginUtils.CurrentTimeInMicroseconds();
                    ulong elapsed = currentTime - _startTime;
                    
                    if (sampleTime > elapsed)
                    {
                        //GD.PrintRich($"[color=green]{realSampleTime - elapsed}[/color]");
                        Task.Delay((int)((sampleTime - elapsed) / 1000)).Wait();
                    }

                    foreach (var id in _sendManager.TrackSet)
                    {
                        SendEncodedData(id, sampleTime, dataPtr, dataSize);
                    }
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
            
            if (NativeMethods.rtcSendMessage(id, dataPtr, size) < 0)
            {
                GD.Print("------------rtcSendMessage Error!");
            }
        }

    }
}

