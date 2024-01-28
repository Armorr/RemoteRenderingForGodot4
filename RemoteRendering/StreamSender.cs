using System;
using System.Collections.Generic;
using Godot;
using Godot.WebRTC;


namespace Godot.RemoteRendering
{
    
    public class SendManager
    {
        private ulong sampleTime_us = 0;
        private readonly ulong sampleDuration_us;

        private HashSet<int> _openedTrackSet = new ();

        public HashSet<int> TrackSet => _openedTrackSet;

        public SendManager(int fps)
        {
            sampleDuration_us = 1000 * 1000 / (ulong)fps;
            sampleTime_us = ulong.MaxValue - sampleDuration_us + 1;
        }

        public void OpenTrack(int id)
        {
            _openedTrackSet.Add(id);
        }

        public void CloseTrack(int id)
        {
            _openedTrackSet.Remove(id);
        }

        public void NextSend()
        {
            sampleTime_us += sampleDuration_us;
        }
        
        public ulong GetSampleTime()
        {
            return sampleTime_us;
        }

        public ulong GetSampleDuration()
        {
            return sampleDuration_us;
        }
        
    }


    public partial class StreamSender : Node
    {
        private static bool _initialized = false;
        
        public StreamSender()
        {
            
        }

    }


}