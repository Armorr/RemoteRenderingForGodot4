using System.IO;



namespace Godot.RemoteRendering.InputSystem
{
    static class MessageSerializer
    {
        
        // public static byte[] Serialize(ref InputRemoting.Message message)
        // {
        //     var stream = new MemoryStream();
        //     var writer = new BinaryWriter(stream);
        //
        //     writer.Write(message.participantId);
        //     writer.Write((int)message.type);
        //     writer.Write(message.data.Length);
        //     writer.Write(message.data);
        //
        //     return stream.ToArray();
        // }

        
        public static void Deserialize(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));
            var participantId = reader.ReadInt32();
            var type = reader.ReadInt32();
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            var s = data.ToString();
            GD.Print(participantId + "  " + type + "  " + length + "  " + s);
            //message = default;
            //message.participantId = reader.ReadInt32();
            //message.type = (InputRemoting.MessageType)reader.ReadInt32();
            //int length = reader.ReadInt32();
            //message.data = reader.ReadBytes(length);
        }
    }
}

