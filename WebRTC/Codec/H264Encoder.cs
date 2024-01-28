using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;


namespace Godot.WebRTC
{
    public unsafe class H264Encoder : VideoEncoder
    {
        private AVCodecContext* _codecContext;
        private SwsContext* _swsContext;
        private AVCodec* _codec;
        private AVPixelFormat _pixelFormat;
        private int _width;
        private int _height;
        private int _defaultSize = 1000;
        private bool _initialized = false;

        public H264Encoder()
        {
            
        }
        
        public override void Initialize(int width, int height, AVPixelFormat pixelFormat, int fps)
        {
            if (!_initialized)
            {
                this._width = width;
                this._height = height;
                this._pixelFormat = pixelFormat;
                
                base.Initialize();
                InitializeEncoder(fps);
                InitializeSwsContext();
                _initialized = true;
            }
        }

        private void InitializeEncoder(int fps)
        {
            _codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
            if (_codec == null)
            {
                throw new Exception("Codec not found.");
            }
            
            _codecContext = ffmpeg.avcodec_alloc_context3(_codec);
            if (_codecContext == null)
            {
                throw new Exception("Could not allocate codec context.");
            }
            
            _codecContext -> width = _width;
            _codecContext -> height = _height;
            _codecContext -> pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            _codecContext -> time_base = new AVRational { num = 1, den = fps };
            _codecContext -> framerate = new AVRational { num = fps, den = 1 };
            _codecContext -> profile = ffmpeg.FF_PROFILE_H264_HIGH;
            
            //_codecContext -> bit_rate = 2000000;
            //_codecContext -> gop_size = 10;
            //_codecContext -> max_b_frames = 10;
            
            //ffmpeg.av_opt_set(_codecContext->priv_data, "preset", "ultrafast", 0);
            //ffmpeg.av_opt_set(_codecContext -> priv_data, "tune", "stillimage", 0);
            ffmpeg.av_opt_set(_codecContext -> priv_data, "tune", "zerolatency", 0);
            
            if (ffmpeg.avcodec_open2(_codecContext, _codec, null) < 0)
            {
                throw new Exception("Could not open codec.");
            }
        }

        private void InitializeSwsContext()
        {
            _swsContext = ffmpeg.sws_getContext(
                _width, _height, _pixelFormat,
                _width, _height, AVPixelFormat.AV_PIX_FMT_YUV420P,
                ffmpeg.SWS_BICUBIC, null, null, null
            );
            if (_swsContext == null)
            {
                throw new Exception("Could not allocate _swsContext.");
            }
        }
        
        public override int EncodeFrame(Image image, long pts, ref IntPtr dataPtr)
        {
            int width = image.GetWidth();
            int height = image.GetHeight();
            
            if (width != _codecContext->width || height != _codecContext->height)
            {
                GD.PrintErr("Input image size does not match codec configuration.");
                return -1;
            }
            
            var srcFrame = CreateFrame(image.GetData(), AVPixelFormat.AV_PIX_FMT_RGB24);
            var dstFrame = CreateFrame(null, AVPixelFormat.AV_PIX_FMT_YUV420P);
            
            ffmpeg.sws_scale(_swsContext, srcFrame->data, srcFrame->linesize, 0, height, dstFrame->data, dstFrame->linesize);
            ffmpeg.av_frame_free(&srcFrame);

            dstFrame -> pts = pts;
            
            var packet = ffmpeg.av_packet_alloc();
            
            if (ffmpeg.avcodec_send_frame(_codecContext, dstFrame) < 0)
            {
                GD.PrintErr("Error sending frame for encoding.");
                return -1;
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
                    totalSize += packet -> size;
                    dataPtr = Marshal.ReAllocHGlobal(dataPtr, (IntPtr)totalSize);
                    Unsafe.CopyBlock(IntPtr.Add(dataPtr, totalSize - packet -> size).ToPointer(), packet->data, (uint)packet -> size);
                }
            } while (!hasFinishedFrame);
            
            
            ffmpeg.av_frame_free(&dstFrame);
            ffmpeg.av_packet_free(&packet);
            
            return totalSize;
        }
        
        private AVFrame* CreateFrame(byte[] data, AVPixelFormat pixFmt)
        {
            var frame = ffmpeg.av_frame_alloc();
            frame->format = (int)pixFmt;
            frame->width = _width;
            frame->height = _height;

            if (ffmpeg.av_frame_get_buffer(frame, 0) < 0)
                throw new Exception("Could not allocate the video frame data.");

            if (data != null)
            {
                Marshal.Copy(data, 0, (IntPtr)frame->data[0], data.Length);
            }

            return frame;
        }
        
    }
}

