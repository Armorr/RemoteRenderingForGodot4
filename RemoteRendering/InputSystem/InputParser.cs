using System;
using System.IO;

namespace Godot.RemoteRendering.InputSystem
{
    public enum MessageType : int
    {
        NEW_MESSAGE = 4,
    }
    
    public static class MessageSerializer
    {
        public static void Deserialize(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));
            var participantId = reader.ReadInt32();
            var type = (MessageType)reader.ReadInt32();
            if (type != MessageType.NEW_MESSAGE)
            {
                return;
            }
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            EventParser.ParseEvent(data);
        }
    }

    public static class EventParser
    {
        public static event Action<InputEvent> OnSendEvent;
        
        public static void ParseEvent(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                InputEvent inputEvent = new InputEvent();
                inputEvent.format = reader.ReadInt32();
                inputEvent.size = reader.ReadInt32();
                inputEvent.deviceId = reader.ReadInt32();
                inputEvent.time = reader.ReadDouble();
                switch (inputEvent.GetFormat())
                {
                    case EventFormat.STAT:
                        StateEvent stateEvent = new StateEvent();
                        stateEvent.format = reader.ReadInt32();
                        stateEvent.stateData = reader.ReadBytes(data.Length - InputEvent.Size - sizeof(int));
                        ParseStateEvent(stateEvent);
                        break;
                    default:
                        GD.PrintErr("Not support this InputEvent yet!");
                        break;
                }
            }
        }

        public static void ParseStateEvent(StateEvent e)
        {
            switch (e.GetFormat())
            {
                case EventFormat.TOUC:
                    var stateEvent = new TouchEvent();
                    using (MemoryStream stateStream = new MemoryStream(e.stateData))
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
                    OnSendEvent?.Invoke(stateEvent);
                    break;
                default:
                    //GD.PrintErr($"Not support this {e.GetFormat()} yet!");
                    break;
            }
            
        }
    }
    
}

