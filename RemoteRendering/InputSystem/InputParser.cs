using System.IO;
using Godot;



namespace Godot.RemoteRendering.InputSystem
{
    static class MessageSerializer
    {
        public static void Deserialize(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));
            var participantId = reader.ReadInt32();
            var type = reader.ReadInt32();
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            var s = data.ToString();
            var e = EventParser.ParseEvent(data);
            GD.Print(e.delta[0]);
            GD.Print(e.delta[1]);
            var ev = new InputEventScreenDrag();
            ev.Relative = new Vector2(e.delta[0], e.delta[1]);
            Input.ParseInputEvent(ev);
        }
    }

    static class EventParser
    {
        public static StateEvent ParseEvent(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                StateEvent stateEvent = new StateEvent();

                // 解析 baseEvent
                stateEvent.baseEvent = InputEvent.Parse(reader.ReadBytes(InputEvent.Size));

                // 解析 stateFormat
                stateEvent.stateFormat = reader.ReadInt32();

                // 解析 stateData
                stateEvent.stateData = reader.ReadBytes(data.Length - InputEvent.Size - sizeof(int));

                // 解析 TouchState 的字段
                using (MemoryStream stateStream = new MemoryStream(stateEvent.stateData))
                using (BinaryReader stateReader = new BinaryReader(stateStream))
                {
                    stateEvent.touchId = stateReader.ReadInt32();
                    stateEvent.position = new float[] { stateReader.ReadSingle(), stateReader.ReadSingle() };
                    stateEvent.delta = new float[] { stateReader.ReadSingle(), stateReader.ReadSingle() };
                    stateEvent.pressure = stateReader.ReadSingle();
                    stateEvent.radius = new float[] { stateReader.ReadSingle(), stateReader.ReadSingle() };
                    stateEvent.phaseId = stateReader.ReadByte();
                    stateEvent.tapCount = stateReader.ReadByte();
                    stateEvent.displayIndex = stateReader.ReadByte();
                    stateEvent.flags = stateReader.ReadByte();
                    stateEvent.padding = stateReader.ReadInt32();
                    stateEvent.startTime = stateReader.ReadDouble();
                    stateEvent.startPosition = new float[] { stateReader.ReadSingle(), stateReader.ReadSingle() };
                }

                return stateEvent;
            }
        }
    }
    
}

