using System;
using System.IO;

namespace ViciNet.Protocol
{
    public abstract class Encodable
    {
        public abstract byte[] Encode();

        protected static byte[] Encode(byte type, Action<BinaryWriter> action)
        {
            // message type: 1byte
            using (var memoryBuffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryBuffer))
                {
                    writer.Write(type);
                    action(writer);
                }

                return memoryBuffer.ToArray();
            }
        }

        protected static T ReadData<T>(byte[] data, Func<BinaryReader, T> fn)
        {
            using (var memoryBuffer = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(memoryBuffer))
                {
                    return fn(reader);
                }
            }
        }
    }
}