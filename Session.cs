using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using ViciNet.Protocol;

namespace ViciNet
{
    public class Session : IDisposable
    {
        // A named request message
        public const byte CmdRequest = 0;

        // An unnamed response message for a request
        public const byte CmdResponse = 1;

        // An unnamed response if requested command is unknown
        public const byte CmdUnknown = 2;

        // A named event registration request
        public const byte EventRegister = 3;

        // A named event de-registration request
        public const byte EventUnregister = 4;

        // An unnamed response for successful event (de-)registration
        public const byte EventConfirm = 5;

        // A unnamed response if event (de-)registration failed
        public const byte EventUnknown = 6;

        // A named event message
        public const byte Event = 7;

        private readonly Transport _transport;

        public Session() : this("/var/run/charon.vici")
        {
        }

        public Session(string path)
        {
            var endPoint = new UnixEndPoint(path);
            _transport = new Transport(endPoint);
        }

        public Session(Socket socket, EndPoint endPoint)
        {
            _transport = new Transport(socket, endPoint);
        }

        private Packet Communicate(Packet packet)
        {
            _transport.Send(packet);
            return _transport.Receive();
        }

        public Message[] Request(string command, params Message[] messages)
        {
            var packet = new Packet(CmdRequest, command, messages);
            var response = Communicate(packet);
            var responseType = response.PacketType;
            if (response.PacketType != CmdResponse)
            {
                throw new InvalidDataException($"packetType: {responseType}");
            }

            return response.Messages;
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}