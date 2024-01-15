using System;
using System.Runtime.InteropServices;
using System.Text;
using Godot.RemoteRendering.InputSystem;

namespace Godot.WebRTC
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RtcReliability
    {
        bool unordered;
        bool unreliable;
        int maxPacketLifeTime; // ignored if reliable
        int maxRetransmits;    // ignored if reliable
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RtcDataChannelInit
    {
        RtcReliability reliability;
        [MarshalAs(UnmanagedType.LPStr)]
        string protocol;  // empty string if NULL
        bool negotiated;
        bool manualStream;
        ushort stream;    // numeric ID 0-65534, ignored if manualStream is false
    }

    public partial class DataChannel : Channel, IDisposable
    {
        private bool disposed;

        private RtcOpenCallbackFunc onOpen;
        private RtcClosedCallbackFunc onClosed;
        private RtcErrorCallbackFunc onError;
        private RtcMessageCallbackFunc onMessage;

        public Action<byte[]> OnInputEvent;

        public DataChannel(int id) : base(id)
        {
            disposed = false;
            InitCallback();
        }

        ~DataChannel()
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

            if (NativeMethods.rtcSetMessageCallback(Id, onMessage) < 0)
                throw new Exception("Error from rtcSetMessageCallback.");
        }

        public new void Dispose()
        {
            //if (!disposed)
                //NativeMethods.rtcDeleteDataChannel(Id);

            //disposed = true;
        }

        private new void OnOpen(int id, IntPtr ptr)
        {
            Opened?.Invoke(id);
            GD.Print("Channel On Open!");
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
            string messageString;
            if (size < 0) {
                messageString = Marshal.PtrToStringAnsi(messagePtr);
            }
            else {
                byte[] messageData = new byte[size];
                Marshal.Copy(messagePtr, messageData, 0, size);
                //messageString = Encoding.UTF8.GetString(messageData);
                OnInputEvent?.Invoke(messageData);
                //MessageSerializer.Deserialize(messageData);
            }
        }

    }


}