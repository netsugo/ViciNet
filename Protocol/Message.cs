using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using ViciNet.Protocol.MessageType;

namespace ViciNet.Protocol
{
    public abstract class Message : Encodable
    {
        // Begin a new section having a name
        protected const byte SectionStart = 1;
        // End a previously started section
        protected const byte SectionEnd = 2;
        // Define a value for a named key in the current section
        protected const byte KeyValue = 3;
        // Begin a name list for list items
        protected const byte ListStart = 4;
        // Define an unnamed item value in the current list
        protected const byte ListItem = 5;
        // End a previously started list
        protected const byte ListEnd = 6;

        public static Message[] Parse(byte[] data)
        {
            return ReadData(data, reader =>
            {
                var count = 0;
                var messages = new List<Message>();
                while (count < data.Length)
                {
                    var type = reader.ReadByte();
                    count += 1;

                    var (message, length) = ReadMessage(reader, type);
                    messages.Add(message);
                    count += length;
                }

                return messages.ToArray();
            });
        }

        protected static void WriteKey(BinaryWriter writer, string key)
        {
            // length header: 1byte
            var len = (byte)key.Length;
            var charArray = key.ToCharArray();
            writer.Write(len);
            writer.Write(charArray);
        }

        protected static void WriteValue(BinaryWriter writer, string value)
        {
            // length header: 2byte
            var len = IPAddress.HostToNetworkOrder((short)value.Length);
            var charArray = value.ToCharArray();
            writer.Write(len);
            writer.Write(charArray);
        }

        private static Tuple<Message, int> ReadMessage(BinaryReader reader, byte type)
        {
            switch (type)
            {
                case KeyValue:
                    return ReadKeyValue(reader);
                case ListStart:
                    return ReadKeyValues(reader);
                case SectionStart:
                    return ReadSection(reader);
                default:
                    throw new InvalidDataException($"MessageType: {type}");
            }
        }

        private static Tuple<Message, int> ReadKeyValue(BinaryReader reader)
        {
            var (key, keyCount) = ReadKey(reader);
            var (value, count) = ReadValue(reader);

            return new Tuple<Message, int>(
                new KeyValueMessage(key, value),
                keyCount + count
            );
        }

        private static Tuple<Message, int> ReadKeyValues(BinaryReader reader)
        {
            var (key, keyCount) = ReadKey(reader);
            var (values, count) = ReadValues(reader);

            return new Tuple<Message, int>(
                new KeyValuesMessage(key, values),
                keyCount + count
            );
        }

        private static Tuple<Message, int> ReadSection(BinaryReader reader)
        {
            var (key, keyCount) = ReadKey(reader);
            var (section, count) = ReadSection(reader);

            return new Tuple<Message, int>(
                new SectionMessage(key, section),
                keyCount + count
            );
        }

        private static Tuple<string, int> ReadKey(BinaryReader reader)
        {
            var length = (int)reader.ReadByte();
            var chars = reader.ReadChars(length);
            var key = new string(chars);
            return new Tuple<string, int>(
                key,
                1 + length
            );
        }

        private static Tuple<string, int> ReadValue(BinaryReader reader)
        {
            var header = reader.ReadBytes(2);
            var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(header, 0));
            var chars = reader.ReadChars(length);
            var value = new string(chars);
            return new Tuple<string, int>(
                value,
                2 + length
            );
        }

        private static Tuple<string[], int> ReadValues(BinaryReader reader)
        {
            var list = new List<string>();
            var count = 0;
            while (true)
            {
                var type = reader.ReadByte();
                count += 1;

                switch (type)
                {
                    case ListItem:
                        var (value, len) = ReadValue(reader);
                        list.Add(value);
                        count += len;
                        break;
                    case ListEnd:
                        return new Tuple<string[], int>(
                            list.ToArray(),
                            count
                        );
                    default:
                        throw new InvalidDataException();
                }
            }
        }

        private static Tuple<Message[], int> ReadMessages(BinaryReader reader)
        {
            var list = new List<Message>();
            var count = 0;
            while (true)
            {
                var type = reader.ReadByte();
                count += 1;

                if (type == SectionEnd)
                {
                    return new Tuple<Message[], int>(
                        list.ToArray(),
                        count
                    );
                }

                var (message, len) = ReadMessage(reader, type);
                list.Add(message);
                count += len;
            }
        }
    }
}
