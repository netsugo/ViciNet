namespace ViciNet.Protocol.MessageType
{
    public class KeyValueMessage : Message
    {
        private readonly string _key;
        private readonly string _value;

        public KeyValueMessage(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override byte[] Encode()
        {
            return Encode(KeyValue, writer =>
            {
                WriteKey(writer, _key);
                WriteValue(writer, _value);
            });
        }

        public override string ToString()
        {
            return $"\"{_key}\":\"{_value}\"";
        }
    }
}
