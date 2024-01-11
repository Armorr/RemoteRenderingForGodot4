using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;


namespace Godot.WebRTC
{
    public class FFmpegBinariesHelper
    {
        internal static void RegisterFFmpegBinaries()
        {
            DynamicallyLoadedBindings.LibrariesPath = "D:\\WebDownloads\\FFmpeg.AutoGen-master\\FFmpeg.AutoGen-master\\FFmpeg\\bin\\x64";
            return;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //var current = System.Environment.CurrentDirectory;
                var current = "D:\\WebDownloads\\FFmpeg.AutoGen-master\\FFmpeg.AutoGen-master";
                var probe = Path.Combine("FFmpeg", "bin", System.Environment.Is64BitProcess ? "x64" : "x86");

                while (current != null)
                {
                    var ffmpegBinaryPath = Path.Combine(current, probe);

                    if (Directory.Exists(ffmpegBinaryPath))
                    {
                        GD.Print($"FFmpeg binaries found in: {ffmpegBinaryPath}");
                        DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaryPath;
                        return;
                    }

                    current = Directory.GetParent(current)?.FullName;
                }
            }
            else
                throw new NotSupportedException(); // fell free add support for platform of your choose
        }
        
        public static unsafe void SetupLogging()
        {
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);

            // do not convert to local function
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

