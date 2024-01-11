using System.Diagnostics;


namespace Godot.WebRTC
{

    public static class PluginUtils
    {

        public static RtcLogCallbackFunc logCallback = OnLog;
        public static void InitLogger(RtcLogLevel level)
        {
            
            NativeMethods.rtcInitLogger(level, logCallback);
        }

        public static void Cleanup()
        {
            // rtcCleanup should come first, otherwise callbacks of cleaned up bridges can be called.
            NativeMethods.rtcCleanup();

            logCallback = null;
        }

        public static void OnLog(RtcLogLevel level, string message)
        {
            GD.Print(level + ": " + message);
        }
        
        public static ulong CurrentTimeInMicroseconds()
        {
            long ticks = Stopwatch.GetTimestamp();
            long microseconds = ticks * 1_000_000L / Stopwatch.Frequency;
            return (ulong)microseconds;
        }
        
    }

}