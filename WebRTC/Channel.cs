using System;
using Godot;
using System.Runtime.InteropServices;
using System.Text;

namespace Godot.WebRTC
{

    public partial class Channel : RefCounted
    {
        public Action<int> Opened { get; set; }
        public Action<int> Closed { get; set; }
        public Action<string> ErrorReceived { get; set; }
        public Action<string> MessageReceived { get; set; }

        public int Id { get; private set; }

        public Channel(int id)
        {
            Id = id;
            //InitCallback();
        }

        public void InitCallback()
        {
            
        }

        public void SendMessage(byte[] message)
        {
            //GCHandle handle = GCHandle.Alloc(message, GCHandleType.Pinned);
            //IntPtr bytes = handle.AddrOfPinnedObject();
            //NativeMethods.rtcSendMessage(Id, bytes, message.Length);
            //handle.Free();
        }

        public void OnOpen(int id, IntPtr ptr)
        {
            
            
        }

        public void OnClosed(int id, IntPtr ptr)
        {
            Closed?.Invoke(id);
        }

        public void OnError(int id, string error, IntPtr ptr)
        {
            ErrorReceived?.Invoke(error);
        }

        public void OnMessage(int id, IntPtr messagePtr, int size, IntPtr ptr)
        {
            
            
        }
    }

}