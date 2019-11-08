using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ViciNet.Protocol
{
    public class Transport : IDisposable
    {
        private const int MaxSegment = 512 * 1024;
        private readonly Socket _socket;

        public Transport(EndPoint endPoint) : this(new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP), endPoint)
        {
        }

        public Transport(Socket socket, EndPoint endPoint)
        {
            _socket = socket;
            _socket.Connect(endPoint);
        }

        public int Send(Packet packet)
        {
            var buffer = SendBuffer(packet);
            return _socket.Send(buffer);
        }

        private static byte[] SendBuffer(Packet packet)
        {
            var encoded = packet.Encode();
            var header = IPAddress.HostToNetworkOrder(encoded.Length);
            using (var memoryBuffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryBuffer))
                {
                    writer.Write(header);
                    writer.Write(encoded);
                }

                return memoryBuffer.ToArray();
            }
        }

        private int ReceiveHeader()
        {
            const int HEADER_SIZE = 4;
            var headerBuffer = new byte[HEADER_SIZE];
            var result = _socket.Receive(headerBuffer, 0, HEADER_SIZE, SocketFlags.None);

            using (var memoryBuffer = new MemoryStream(headerBuffer))
            {
                using (var reader = new BinaryReader(memoryBuffer))
                {
                    var data = reader.ReadInt32();
                    return IPAddress.NetworkToHostOrder(data);
                }
            }
        }

        private byte[] ReceiveData(int length)
        {
            var buffer = new byte[MaxSegment];
            var sum = 0;
            while (sum < length)
            {
                sum += _socket.Receive(buffer, sum, length, SocketFlags.None);
            }

            if (sum > length)
            {
                throw new InvalidDataException($"invalid size: {sum} (expected:{length})");
            }

            using (var memoryBuffer = new MemoryStream(buffer))
            {
                using (var reader = new BinaryReader(memoryBuffer))
                {
                    return reader.ReadBytes(length);
                }
            }
        }

        public Packet Receive()
        {
            var dataLen = ReceiveHeader();
            var data = ReceiveData(dataLen);
            return new Packet(data);
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}