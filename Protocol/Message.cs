using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

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

        private static string MessageType(byte type)
        {
            switch (type)
            {
                case 1: return "section-start";
                case 2: return "section-end";
                case 3: return "key-value";
                case 4: return "list-start";
                case 5: return "list-item";
                case 6: return "list-end";
                default: return "undefined";
            }
        }

        private static Exception MessageTypeException(byte type)
        {
            return new InvalidDataException($"MessageType: {type}(" + MessageType(type) + ')');
        }

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
                    return ReadKeyArray(reader);
                case SectionStart:
                    return ReadSection(reader);
                default:
                    throw MessageTypeException(type);
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

        private static Tuple<Message, int> ReadKeyArray(BinaryReader reader)
        {
            var (key, keyCount) = ReadKey(reader);
            var (values, count) = ReadArray(reader);

            return new Tuple<Message, int>(
                new KeyArrayMessage(key, values),
                keyCount + count
            );
        }

        private static Tuple<Message, int> ReadSection(BinaryReader reader)
        {
            var (key, keyCount) = ReadKey(reader);
            var (section, count) = ReadMessages(reader);

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

        private static Tuple<string[], int> ReadArray(BinaryReader reader)
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


    public class KeyValueMessage : Message
    {
        public string Key { get; }
        public string Value { get; }

        public KeyValueMessage(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public override byte[] Encode()
        {
            return Encode(KeyValue, writer =>
            {
                WriteKey(writer, Key);
                WriteValue(writer, Value);
            });
        }

        public override string ToString()
        {
            return $"\"{Key}\":\"{Value}\"";
        }
    }


    public class KeyArrayMessage : Message
    {
        public string Key;
        public string[] Values;

        public KeyArrayMessage(string key, params string[] values)
        {
            Key = key;
            Values = values;
        }

        public override byte[] Encode()
        {
            return Encode(ListStart, writer =>
            {
                // key length header: 1byte
                WriteKey(writer, Key);
                foreach (var value in Values)
                {
                    // message type: 1byte
                    writer.Write(ListItem);
                    WriteValue(writer, value);
                }

                writer.Write(ListEnd);
            });
        }

        public override string ToString()
        {
            string values;
            switch (Values.Length)
            {
                case 0:
                    values = null;
                    break;
                case 1:
                    values = "\"" + Values[0] + "\"";
                    break;
                default:
                    values = Values.Select(value => $"\"{value}\"").Aggregate((s1, s2) => $"{s1},{s2}");
                    break;
            }

            return $"\"{Key}\":[{values}]";
        }
    }


    public class SectionMessage : Message
    {
        private readonly string _key;
        private readonly Message[] _messages;

        public SectionMessage(string key, params Message[] section)
        {
            _key = key;
            _messages = section;
        }

        public override byte[] Encode()
        {
            return Encode(SectionStart, writer =>
            {
                // key length header: 1byte
                WriteKey(writer, _key);
                foreach (var message in _messages)
                {
                    // message type: 1byte
                    var encoded = message.Encode();
                    writer.Write(encoded);
                }

                writer.Write(SectionEnd);
            });
        }

        public override string ToString()
        {
            string section;
            switch (_messages.Length)
            {
                case 0:
                    section = null;
                    break;
                case 1:
                    section = _messages[0].ToString();
                    break;
                default:
                    section = _messages.Select(msg => msg.ToString()).Aggregate((m1, m2) => $"{m1},{m2}");
                    break;
            }

            return $"\"{_key}\":{{{section}}}";
        }
    }
}