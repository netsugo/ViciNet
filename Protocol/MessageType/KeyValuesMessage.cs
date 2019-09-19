﻿using System.Linq;

namespace ViciNet.Protocol.MessageType
{
    public class KeyValuesMessage : Message
    {
        private readonly string _key;
        private readonly string[] _values;

        public KeyValuesMessage(string key, params string[] values)
        {
            _key = key;
            _values = values;
        }

        public override byte[] Encode()
        {
            return Encode(ListStart, writer =>
            {
                // key length header: 1byte
                WriteKey(writer, _key);
                foreach (var value in _values)
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
            switch (_values.Length)
            {
                case 0:
                    values = null;
                    break;
                case 1:
                    values = _values[0];
                    break;
                default:
                    values = _values.Select(value => $"\"{value}\"").Aggregate((s1, s2) => $"{s1},{s2}");
                    break;
            }

            return $"\"{_key}\" = [{values}]";
        }
    }
}