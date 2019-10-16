using System;
using System.IO;

namespace ViciNet.Protocol
{
    public class Packet : Encodable
    {
        private static readonly bool[] IsNamedList = { true, false, false, true, true, false, false, true };

        public readonly byte PacketType;
        private readonly string _name;
        public readonly Message[] Messages;

        public Packet(byte packetType, string name, params Message[] messages)
        {
            PacketType = packetType;
            _name = name;
            Messages = messages;
        }

        public Packet(byte[] data)
        {
            (PacketType, _name, Messages) = ReadPacket(data);
        }

        public override byte[] Encode()
        {
            return Encode(PacketType, writer =>
            {
                writer.Write(_name);

                // ignored if Messages is empty
                foreach (var message in Messages)
                {
                    writer.Write(message.Encode());
                }
            });
        }

        private static Tuple<byte, string, Message[]> ReadPacket(byte[] data)
        {
            return ReadData(data, reader =>
            {
                var packetType = reader.ReadByte();
                var (name, nameLen) = IsNamedList[packetType]
                    ? ReadName(reader)
                    : new Tuple<string, int>(null, 0);

                var remain = data.Length - (1 + nameLen);
                var messages = remain == 0
                    ? new Message[] { }
                    : ReadMessages(reader, remain);

                return new Tuple<byte, string, Message[]>(packetType, name, messages);
            });
        }

        private static Tuple<string, int> ReadName(BinaryReader reader)
        {
            var nameLen = (int)reader.ReadByte();
            var charArray = reader.ReadChars(nameLen);

            return new Tuple<string, int>(new string(charArray), 1 + nameLen);
        }

        private static Message[] ReadMessages(BinaryReader reader, int length)
        {
            var encodedMessage = reader.ReadBytes(length);
            return Message.Parse(encodedMessage);
        }
    }
}