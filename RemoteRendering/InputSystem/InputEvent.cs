using System.IO;


namespace Godot.RemoteRendering.InputSystem
{
    public class InputEvent
    {
        public int format;
        public int size;
        public int deviceId;
        public double time;

        public const int Size = 20;  // 输入事件的大小

        public static InputEvent Parse(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                InputEvent inputEvent = new InputEvent();
                inputEvent.format = reader.ReadInt32();
                inputEvent.size = reader.ReadInt32();
                inputEvent.deviceId = reader.ReadInt32();
                inputEvent.time = reader.ReadDouble();
                return inputEvent;
            }
        }
    }
    
    public class StateEvent
    {
        public InputEvent baseEvent;
        public int stateFormat;
        public byte[] stateData;

        // TouchState 的字段
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

