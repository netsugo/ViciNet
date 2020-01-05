using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace ViciNet.Protocol
{
    public static class BinarySerde
    {
        public static void WriteKey(this BinaryWriter writer, string key)
        {
            // length header: 1byte
            var len = (byte)key.Length;
            var charArray = key.ToCharArray();
            writer.Write(len);
            writer.Write(charArray);
        }

        public static void WriteValue(this BinaryWriter writer, string value)
        {
            // length header: 2byte
            var len = IPAddress.HostToNetworkOrder((short)value.Length);
            var charArray = value.ToCharArray();
            writer.Write(len);
            writer.Write(charArray);
        }

        public static byte[] Encode(Action<BinaryWriter> action)
        {
            // message type: 1byte
            using (var memoryBuffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryBuffer))
                {
                    action(writer);
                }

                return memoryBuffer.ToArray();
            }
        }

        public static byte[] Encode(string key, string value)
        {
            return Encode(writer => 
            {
                writer.Write(Message.KeyValue);
                writer.WriteKey(key);
                writer.WriteValue(value);
            });
        }

        public static byte[] Encode(string key, IEnumerable<string> array)
        {
            return Encode(writer =>
            {
                writer.Write(Message.ListStart);
                writer.WriteKey(key); ;
                foreach (var value in array)
                {
                    // message type: 1byte
                    writer.Write(Message.ListItem);
                    writer.WriteValue(value);
                }

                writer.Write(Message.ListEnd);
            });
        }

        public static byte[] Encode(string key, IEnumerable<Message> sectionValues)
        {
            return Encode(writer =>
            {
                writer.Write(Message.SectionStart);
                // key length header: 1byte
                writer.WriteKey(key);
                foreach (var message in sectionValues)
                {
                    // message type: 1byte
                    var encoded = message.Encode();
                    writer.Write(encoded);
                }

                writer.Write(Message.SectionEnd);
            });
        }

        public static T Parse<T>(byte[] data, Func<BinaryReader, T> fn)
        {
            using (var memoryBuffer = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(memoryBuffer))
                {
                    return fn(reader);
                }
            }
        }

        public static Tuple<string, int> ReadKey(this BinaryReader reader)
        {
            var length = (int)reader.ReadByte();
            var chars = reader.ReadChars(length);
            var key = new string(chars);
            return new Tuple<string, int>(
                key,
                1 + length
            );
        }

        public static Tuple<string, int> ReadValue(this BinaryReader reader)
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

        public static Tuple<string[], int> ReadArray(this BinaryReader reader)
        {
            var list = new List<string>();
            var count = 0;
            while (true)
            {
                var type = reader.ReadByte();
                count += 1;

                switch (type)
                {
                    case Message.ListItem:
                        var (value, len) = reader.ReadValue();
                        list.Add(value);
                        count += len;
                        break;
                    case Message.ListEnd:
                        return new Tuple<string[], int>(
                            list.ToArray(),
                            count
                        );
                    default:
                        throw new InvalidDataException();
                }
            }
        }

        public static Tuple<Message[], int> ReadSectionValues(this BinaryReader reader)
        {
            var list = new List<Message>();
            var count = 0;
            while (true)
            {
                var type = reader.ReadByte();
                count += 1;

                switch (type)
                {
                    case Message.SectionEnd:
                        return new Tuple<Message[], int>(
                            list.ToArray(),
                            count
                        );
                    default:
                        var (message, len) = reader.ReadMessage(type);
                        list.Add(message);
                        count += len;
                        break;
                }
            }
        }

        public static Tuple<Message, int> ReadKeyValue(this BinaryReader reader)
        {
            var (key, keyCount) = reader.ReadKey();
            var (value, count) = reader.ReadValue();

            return new Tuple<Message, int>(
                new KeyValueMessage(key, value),
                keyCount + count
            );
        }

        public static Tuple<Message, int> ReadKeyArray(this BinaryReader reader)
        {
            var (key, keyCount) = reader.ReadKey();
            var (values, count) = reader.ReadArray();

            return new Tuple<Message, int>(
                new KeyArrayMessage(key, values),
                keyCount + count
            );
        }

        public static Tuple<Message, int> ReadSection(this BinaryReader reader)
        {
            var (key, keyCount) = reader.ReadKey();
            var (section, count) = reader.ReadSectionValues();

            return new Tuple<Message, int>(
                new SectionMessage(key, section),
                keyCount + count
            );
        }

        public static Tuple<Message, int> ReadMessage(this BinaryReader reader, byte type)
        {
            switch (type)
            {
                case Message.KeyValue:
                    return reader.ReadKeyValue();
                case Message.ListStart:
                    return reader.ReadKeyArray();
                case Message.SectionStart:
                    return reader.ReadSection();
                default:
                    return null;
            }
        }
    }
}