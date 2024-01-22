using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot.WebRTC;
using Godot.RemoteRendering.InputSystem;


namespace Godot.RemoteRendering
{
    public class InputReceiver
    {
        private Dictionary<int, DataChannel> _mapIdDatachannel = new ();
        
        public InputReceiver()
        {
            EventParser.OnSendEvent += InputManager.HandleEvent;
        }

        public void SetChannel(DataChannel dc)
        {
            if (_mapIdDatachannel.ContainsKey(dc.Id))
            {
                GD.Print($"Input Receiver Already has this datachannel {dc.Id}!");
            }
            else
            {
                _mapIdDatachannel.Add(dc.Id, dc);
                dc.OnInputEvent += OnMessage;
            }
        }

        public void RemoveChannel()
        {
            
        }
        
        public static void OnMessage(byte[] messageData)
        {
            MessageSerializer.Deserialize(messageData);
        }
        
    }
}

