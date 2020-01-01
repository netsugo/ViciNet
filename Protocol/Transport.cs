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
            return BinarySerde.Encode(writer =>
            {
                writer.Write(header);
                writer.Write(encoded);
            });
        }

        private int ReceiveHeader()
        {
            const int HEADER_SIZE = 4;
            var headerBuffer = new byte[HEADER_SIZE];
            var length = HEADER_SIZE;
            var sum = 0;
            while (sum < length)
            {
                var received = _socket.Receive(headerBuffer, sum, length - sum, SocketFlags.None);
                sum += received;
            }

            return BinarySerde.Parse(headerBuffer, reader =>
            {
                var data = reader.ReadInt32();
                return IPAddress.NetworkToHostOrder(data);
            });
        }

        private byte[] ReceiveData(int length)
        {
            var buffer = new byte[MaxSegment];
            var sum = 0;
            while (sum < length)
            {
                var received = _socket.Receive(buffer, sum, length - sum, SocketFlags.None);
                sum += received;
            }

            if (sum > length)
            {
                throw new InvalidDataException($"invalid size: {sum} (expected:{length})");
            }

            return BinarySerde.Parse(buffer, reader =>
            {
                return reader.ReadBytes(length);
            });
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