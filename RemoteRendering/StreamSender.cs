using System;
using System.Collections.Generic;
using Godot;
using Godot.WebRTC;


namespace Godot.RemoteRendering
{

    public interface IStreamSender
    {
        

    }


    public partial class StreamSender : Node3D
    {

        private readonly Dictionary<int, Track> _mapPeerIdAndTrack = new Dictionary<int, Track>();

        public StreamSender()
        {
            
        }

    }


}