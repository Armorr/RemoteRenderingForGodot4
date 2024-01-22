using System.Text;
using System.IO;
using System;


namespace Godot.RemoteRendering.InputSystem
{
    public enum EventFormat
    {
        STAT,
        TOUC,
        TSCR,
        MOUS,
        KEYS,
        GPAD,
        TEXT
    }
    
    public class InputEvent
    {
        public int format;
        public int size;
        public int deviceId;
        public double time;

        public const int Size = 20;
        
        public EventFormat GetFormat()
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(format >> 24);
            bytes[1] = (byte)(format >> 16);
            bytes[2] = (byte)(format >> 8);
            bytes[3] = (byte)format;
            return (EventFormat)Enum.Parse(typeof(EventFormat), Encoding.ASCII.GetString(bytes));
        }
    }
    
    public class StateEvent : InputEvent
    {
        public byte[] stateData;
    }

    public class TouchEvent : StateEvent
    {
        public int touchId;
        public float[] position;
        public float[] delta;
        public float pressure;
        public float[] radius;
        public byte phaseId;
        public byte tapCount;
        public byte displayIndex;
        public byte flags;
        public int padding;
        public double startTime;
        public float[] startPosition;
    }
    
}

