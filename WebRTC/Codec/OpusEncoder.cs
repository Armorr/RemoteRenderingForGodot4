using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;


namespace Godot.WebRTC
{
    public unsafe class OpusEncoder : AudioEncoder
    {
        private AVCodecContext* _codecContext;
        private AVCodec* _codec;
        private int _sampleRate;
        private int _channels;
        private int _frameSize;
        private int _defaultSize = 1000;
        private bool _initialized = false;

        public OpusEncoder()
        {
            
        }

        public override void Initialize(int sampleRate, int channels)
        {
            if (!_initialized)
            {
                _channels = channels + 1;
                _sampleRate = sampleRate;
                InitializeEncoder();
                _initialized = true;
            }
        }

        public void InitializeEncoder()
        {
            _codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_OPUS);
            if (_codec == null)
            {
                throw new Exception("Opus codec not found.");
            }
        
            _codecContext = ffmpeg.avcodec_alloc_context3(_codec);
            if (_codecContext == null)
            {
                throw new Exception("Could not allocate Opus codec context.");
            }
        
            _codecContext -> sample_rate = _sampleRate;
            _codecContext -> ch_layout.nb_channels = _channels;
            //_codecContext -> bit_rate = 300000;
            _codecContext -> sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLT;
            
            ffmpeg.av_opt_set(_codecContext -> priv_data, "tune", "zerolatency", 0);

            if (ffmpeg.avcodec_open2(_codecContext, _codec, null) < 0)
            {
                throw new Exception("Could not open Opus codec.");
            }
            
            _frameSize = _codecContext->frame_size;
        }

        public override int EncodeFrame(Vector2[] source, ref IntPtr dataPtr)
        {
            if (source == null)
            {
                return 0;
            }
            var pcmSamples = ConvertVector2ToFloatArray(source);
            
            var packet = ffmpeg.av_packet_alloc();
            var frame = ffmpeg.av_frame_alloc();
            
            frame->ch_layout.nb_channels = _channels;
            frame->nb_samples = _frameSize;
            frame->format = (int)_codecContext->sample_fmt;
            
            if (ffmpeg.av_frame_get_buffer(frame, 0) < 0)
            {
                throw new Exception("Could not allocate audio frame data.");
            }
            
            Marshal.Copy(pcmSamples, 0, (IntPtr)frame->data[0], pcmSamples.Length);
            
            if (ffmpeg.avcodec_send_frame(_codecContext, frame) < 0)
            {
                throw new Exception("Error sending frame for Opus encoding.");
            }
            
            int result;
            int totalSize = 0;
            bool isPacketValid = false;
            bool hasFinishedFrame = false;
            
            do
            {
                ffmpeg.av_packet_unref(packet);
                result = ffmpeg.avcodec_receive_packet(_codecContext, packet);

                if (result == 0)
                {
                    isPacketValid = true;
                    hasFinishedFrame = false;
                }
                else if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    isPacketValid = false;
                    hasFinishedFrame = true;
                }
                else if (result == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                {
                    isPacketValid = false;
                    hasFinishedFrame = true;
                }
                else
                {
                    throw new InvalidOperationException($"Error from avcodec_receive_packet: {result}");
                }

                if (isPacketValid)
                {
                    totalSize += packet->size;
                    dataPtr = Marshal.ReAllocHGlobal(dataPtr, (IntPtr)totalSize);
                    Unsafe.CopyBlock(IntPtr.Add(dataPtr, totalSize - packet->size).ToPointer(), packet->data, (uint)packet->size);
                }
            } while (!hasFinishedFrame);

            ffmpeg.av_frame_free(&frame);
            ffmpeg.av_packet_free(&packet);
            
            return totalSize;
        }
        
        private float[] ConvertVector2ToFloatArray(Vector2[] audioData)
        {
            var samples = new float[audioData.Length * _channels];

            for (int i = 0; i < audioData.Length; i++)
            {
                samples[i * _channels] = audioData[i].X;
                samples[i * _channels + 1] = audioData[i].Y;
            }

            return samples;
        }
        
    }
}

