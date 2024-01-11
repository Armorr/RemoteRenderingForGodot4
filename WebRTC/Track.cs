using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Godot.WebRTC
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RtcTrackInit
    {
        public RtcDirection direction;
        public RtcCodec codec;
        public int payloadType;
        public uint ssrc;
        [MarshalAs(UnmanagedType.LPStr)]
        public string mid;
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;    // optional
        [MarshalAs(UnmanagedType.LPStr)]
        public string msid;    // optional
        [MarshalAs(UnmanagedType.LPStr)]
        public string trackId; // optional, track ID used in MSID
        [MarshalAs(UnmanagedType.LPStr)]
        public string profile; // optional, codec profile
    }


    public partial class Track : Channel, IDisposable
    {
        private bool disposed;

        private RtcOpenCallbackFunc onOpen;
        private RtcClosedCallbackFunc onClosed;
        private RtcErrorCallbackFunc onError;
        private RtcMessageCallbackFunc onMessage;
        
        public Track(int id) : base(id)
        {
            disposed = false;
            InitCallback();
        }

        ~Track()
        {
            Dispose();
        }

        public new void InitCallback()
        {
            onOpen = OnOpen;
            onClosed = OnClosed;
            onError = OnError;
            onMessage = OnMessage;
            if (NativeMethods.rtcSetOpenCallback(Id, onOpen) < 0)
                throw new Exception("Error from rtcSetOpenCallback.");

            if (NativeMethods.rtcSetClosedCallback(Id, onClosed) < 0)
                throw new Exception("Error from rtcSetClosedCallback.");

            if (NativeMethods.rtcSetErrorCallback(Id, onError) < 0)
                throw new Exception("Error from rtcSetErrorCallback.");

            //if (NativeMethods.rtcSetMessageCallback(Id, onMessage) < 0)
            //    throw new Exception("Error from rtcSetMessageCallback.");
        }

        public new void Dispose()
        {
            if (!disposed)
                NativeMethods.rtcDeleteTrack(Id);

            disposed = true;
        }

        private new void OnOpen(int id, IntPtr ptr)
        {
            Opened?.Invoke(id);
        }

        private new void OnClosed(int id, IntPtr ptr)
        {
            Closed?.Invoke(id);
        }

        private new void OnError(int id, string error, IntPtr ptr)
        {
            ErrorReceived?.Invoke(error);
        }

        private new void OnMessage(int id, IntPtr messagePtr, int size, IntPtr ptr)
        {
            
        }

    }

}