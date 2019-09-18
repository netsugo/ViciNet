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

        public Packet Receive()
        {
            var buffer = new byte[MaxSegment];
            var result = _socket.Receive(buffer);
            return ReadPacket(buffer);
        }

        private Packet ReadPacket(byte[] buffer)
        {
            using (var memoryBuffer = new MemoryStream(buffer))
            {
                using (var reader = new BinaryReader(memoryBuffer))
                {
                    var header = reader.ReadInt32();
                    var data = reader.ReadBytes(IPAddress.NetworkToHostOrder(header));
                    return new Packet(data);
                }
            }
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}