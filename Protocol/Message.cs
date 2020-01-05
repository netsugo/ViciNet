using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciNet.Protocol
{
    public abstract class Message : IEncodable
    {
        // Begin a new section having a name
        public const byte SectionStart = 1;

        // End a previously started section
        public const byte SectionEnd = 2;

        // Define a value for a named key in the current section
        public const byte KeyValue = 3;

        // Begin a name list for list items
        public const byte ListStart = 4;

        // Define an unnamed item value in the current list
        public const byte ListItem = 5;

        // End a previously started list
        public const byte ListEnd = 6;

        public static Message[] Parse(byte[] data)
        {
            return BinarySerde.Parse(data, reader =>
            {
                var count = 0;
                var messages = new List<Message>();
                while (count < data.Length)
                {
                    var type = reader.ReadByte();
                    count += 1;

                    var (message, length) = reader.ReadMessage(type);
                    messages.Add(message);
                    count += length;
                }

                return messages.ToArray();
            });
        }

        public abstract byte[] Encode();
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
            return BinarySerde.Encode(Key, Value);
        }

        public override string ToString()
        {
            return $"\"{Key}\":\"{Value}\"";
        }
    }


    public class KeyArrayMessage : Message
    {
        public string Key { get; }
        public string[] Values { get; }

        public KeyArrayMessage(string key, params string[] values)
        {
            Key = key;
            Values = values;
        }

        public override byte[] Encode()
        {
            return BinarySerde.Encode(Key, Values);
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
        public string Key { get; }
        public Message[] Messages { get; }

        public SectionMessage(string key, params Message[] section)
        {
            Key = key;
            Messages = section;
        }

        public override byte[] Encode()
        {
            return BinarySerde.Encode(Key, Messages);
        }

        public override string ToString()
        {
            string section;
            switch (Messages.Length)
            {
                case 0:
                    section = null;
                    break;
                case 1:
                    section = Messages[0].ToString();
                    break;
                default:
                    section = Messages.Select(msg => msg.ToString()).Aggregate((m1, m2) => $"{m1},{m2}");
                    break;
            }

            return $"\"{Key}\":{{{section}}}";
        }
    }
}