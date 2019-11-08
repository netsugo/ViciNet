using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using ViciNet.Protocol;
using ViciNet.RequestAttribute;

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

        private static string PacketType(byte type)
        {
            switch (type)
            {
                case 0: return "command-request";
                case 1: return "command-response";
                case 2: return "event-unknown";
                case 3: return "event-register";
                case 4: return "event-unregister";
                case 5: return "event-confirm";
                case 6: return "event-unknown";
                case 7: return "event";
                default: return "undefined";
            }
        }

        private static Exception PacketTypeException(byte packetType)
        {
            return new InvalidDataException($"packet-type:{packetType}" + '(' + PacketType(packetType) + ')');
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
            if (responseType != CmdResponse)
            {
                throw PacketTypeException(responseType);
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
            var packetType = response.PacketType;

            switch (packetType)
            {
                case EventUnknown:
                    throw PacketTypeException(packetType);
                case EventConfirm:
                    return;
                default:
                    throw PacketTypeException(packetType);
            }
        }

        public Message[][] StreamedRequest(string command, string eventStreamType, params Message[] messages)
        {
            var messagesList = new List<Message[]>();
            var packet = new Packet(CmdRequest, command, messages);
            RegisterEvent(eventStreamType);
            var sendCode = _transport.Send(packet);
            while (true)
            {
                var response = _transport.Receive();
                var packetType = response.PacketType;

                var receivedMessages = response.Messages;
                switch (packetType)
                {
                    case CmdResponse:
                        UnregisterEvent(eventStreamType);
                        messagesList.Add(receivedMessages);
                        return messagesList.ToArray();
                    case Event:
                        messagesList.Add(receivedMessages);
                        break;
                    default:
                        UnregisterEvent(eventStreamType);
                        throw PacketTypeException(packetType);

                }
            }
        }

        public Message[] Request(Command command, params Message[] messages)
        {
            return Request(command.GetName(), messages);
        }

        public Message[][] StreamedRequest(Command command, StreamEvent eventStreamType, params Message[] messages)
        {
            return StreamedRequest(command.GetName(), eventStreamType.GetName(), messages);
        }

        public Message[][] StreamedRequest(Command command, params Message[] messages)
        {
            return CommandToEventTable.TryGetValue(command, out var streamEvent)
                ? StreamedRequest(command, streamEvent, messages)
                : throw new ArgumentException($"\"{command.GetName()}\" isn't stream command.");
        }

        private static readonly Dictionary<Command, StreamEvent> CommandToEventTable = new Dictionary<Command, StreamEvent>
        {
            {
                Command.ListSas,
                StreamEvent.ListSa
            },
            {
                Command.ListPolicies,
                StreamEvent.ListPolicy
            },
            {
                Command.ListConns,
                StreamEvent.ListConn
            },
            {
                Command.ListCerts,
                StreamEvent.ListCert
            },
            {
                Command.ListAuthorities,
                StreamEvent.ListAuthority
            }
        };

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}