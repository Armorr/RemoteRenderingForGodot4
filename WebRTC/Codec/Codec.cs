using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;


namespace Godot.WebRTC
{

    public class Encoder
    {
        private static bool Initialized = false;
        public void Initialize()
        {
            if (!Initialized)
            {
                FFmpegBinariesHelper.RegisterFFmpegDlls();
                DynamicallyLoadedBindings.Initialize();
                FFmpegBinariesHelper.SetupLogging();
                Initialized = true;
            }
        }
    }

    public class VideoEncoder : Encoder
    {
        public virtual void Initialize(int width, int height, AVPixelFormat pixelFormat, int fps)
        {
            base.Initialize();
        }

        public virtual int EncodeFrame(Image image, long pts, ref IntPtr dataPtr)
        {
            return -1;
        }
    }

    public class AudioEncoder : Encoder
    {
        public virtual void Initialize(int sampleRate, int channels)
        {
            base.Initialize();
        }

        public virtual int EncodeFrame(Vector2[] source, ref IntPtr dataPtr)
        {
            return -1;
        }
    }
    
    public class FFmpegBinariesHelper
    {
        internal static void RegisterFFmpegDlls()
        {
            DynamicallyLoadedBindings.LibrariesPath = ".\\FFmpegLibraries";
        }
        
        public static unsafe void SetupLogging()
        {
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
            
            av_log_set_callback_callback logCallback = (p0, level, format, vl) =>
            {
                if (level > ffmpeg.av_log_get_level()) return;

                var lineSize = 1024;
                var lineBuffer = stackalloc byte[lineSize];
                var printPrefix = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(line);
                Console.ResetColor();
            };

            ffmpeg.av_log_set_callback(logCallback);
        }
        
    }
}

