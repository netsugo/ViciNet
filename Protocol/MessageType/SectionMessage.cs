using System.Linq;

namespace ViciNet.Protocol.MessageType
{
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
            var section = _messages.Length > 0
                ? _messages.Select(msg => msg.ToString()).Aggregate((m1, m2) => $"{m1},{m2}")
                : null;
            return $"\"{_key}\":{{{section}}}";
        }
    }
}