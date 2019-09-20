using System;
using System.Collections.Generic;
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
        
        private void RegisterEvent(string eventStreamType)
        {
            RegisterUnregister(eventStreamType, EventRegister);
        }

        private void UnregisterEvent(string eventStreamType)
        {
            RegisterUnregister(eventStreamType, EventUnregister);
        }

        private void RegisterUnregister(string eventStreamType, byte registerType)
        {
            var packet = new Packet(registerType, eventStreamType);
            var response = Communicate(packet);

            if (response.PacketType == EventUnknown)
            {
                throw new InvalidDataException();
            }

            if (response.PacketType != EventConfirm)
            {
                throw new InvalidDataException();
            }
        }

        public Message[][] StreamedRequest(string command, string eventStreamType, params Message[] messages)
        {
            Console.WriteLine("command:{0}", command);
            Console.WriteLine("event:{0}", eventStreamType);
            var messagesList = new List<Message[]>();
            var packet = new Packet(CmdRequest, command, messages);
            RegisterEvent(eventStreamType);
            var sendCode = _transport.Send(packet);
            while (true)
            {
                var response = _transport.Receive();
                if (response.PacketType != Event)
                {
                    UnregisterEvent(eventStreamType);
                    if (response.PacketType != CmdResponse)
                    {
                        throw new InvalidDataException();
                    }

                    messagesList.Add(response.Messages);
                    return messagesList.ToArray();
                }

                messagesList.Add(response.Messages);
            }
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}