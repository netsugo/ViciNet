using System;
using System.Collections.Generic;
using System.Linq;
using ViciNet.Protocol;
using ViciNet.RequestAttribute;

namespace ViciNet
{
    public static class CommandExtensions
    {
        // version
        // stats
        // reload-settings

        /// <param name = "type">child | ike</param>
        /// <param name = "name">configuration name</param>
        public static void Initiate(this Session session, string type, string name)
        {
            VoidCommandRequest(session, Command.Initiate, new KeyValueMessage(type, name));
        }

        /// <param name = "type">child | ike</param>
        /// <param name = "name">configuration name</param>
        public static Tuple<int, int> Terminate(this Session session, string type, string name)
        {
            return TypedCommandRequest(session, Command.Terminate, new[] { new KeyValueMessage(type, name) }, messages =>
            {
                var matches = (messages[1] as KeyValueMessage).Value;
                var terminated = (messages[2] as KeyValueMessage).Value;
                return new Tuple<int, int>(int.Parse(matches), int.Parse(terminated));
            });
        }

        // rekey
        // redirect
        // install
        // uninstall

        public static Message[][] ListSas(this Session session)
        {
            return session.StreamedRequest(Command.ListSas);
        }

        public static Message[][] ListSas(this Session session, string ike)
        {
            return session.StreamedRequest(Command.ListSas, new KeyValueMessage("ike", ike));
        }

        // list-policies

        public static Message[][] ListConns(this Session session, string ike)
        {
            return session.StreamedRequest(Command.ListConns, new KeyValueMessage("ike", ike));
        }

        public static IEnumerable<string> GetConns(this Session session)
        {
            return GetCommandRequest<IEnumerable<string>>(session, Command.GetConns, messages => (messages[0] as KeyArrayMessage).Values);
        }

        // type: X509 | X509_AC | X509_CRL | OCSP_RESPONSE | PUBKEY | ANY
        // flag: NONE | CA | AA | OCSP | ANY
        // subject: set to list only certificates having subject
        public static Message[][] ListCerts(this Session session, string type, string flag, string subject = "")
        {
            var typeMessage = new KeyValueMessage("type", type);
            var flagMessage = new KeyValueMessage("flag", flag);
            var subjectMessage = new KeyValueMessage("subject", subject);
            return session.StreamedRequest(Command.ListCerts, typeMessage, flagMessage, subjectMessage);
        }

        // list-authorities

        public static void LoadConn(this Session session, Message config)
        {
            VoidCommandRequest(session, Command.LoadConn, config);
        }

        public static void UnloadConn(this Session session, string name)
        {
            VoidCommandRequest(session, Command.UnloadConn, new KeyValueMessage("name", name));
        }

        /// <param name = "type">X509 | X509_AC | X509_CRL</param>
        /// <param name = "flag">NONE | CA | AA | OCSP</param>
        /// <param name = "data">PEM or DER encoded key data</param>
        public static void LoadCert(this Session session, string type, string flag, string data)
        {
            VoidCommandRequest(session, Command.LoadCert, new KeyValueMessage("type", type), new KeyValueMessage("flag", flag), new KeyValueMessage("data", data));
        }

        /// <param name = "type">rsa | ecdsa | bliss | any</param>
        /// <param name = "data">PEM or DER encoded key data</param>
        public static string LoadKey(this Session session, string type, string data)
        {
            var typeMessage = new KeyValueMessage("type", type);
            var dataMessage = new KeyValueMessage("data", data);

            return TypedCommandRequest(session, Command.LoadKey, new[] { typeMessage, dataMessage }, messages => (messages[1] as KeyValueMessage).Value);
        }

        public static void UnloadKey(this Session session, string id)
        {
            VoidCommandRequest(session, Command.UnloadKey, new KeyValueMessage("id", id));
        }

        public static string[] GetKeys(this Session session)
        {
            return GetCommandRequest(session, Command.GetKeys, messages => (messages[0] as KeyArrayMessage).Values);
        }

        // load-token
        // load-shared
        // unload-shared
        // get-shared

        /// <param name = "type">X509 | X509_AC | X509_CRL | OCSP_RESPONSE | PUBKEY | ANY</param>
        public static void FlushCerts(this Session session, string type)
        {
            VoidCommandRequest(session, Command.FlushCerts, new KeyValueMessage("type", type));
        }

        // clear-creds
        // load-authority
        // unload-authority
        // load-pool
        // unload-pool
        // get-pools
        // get-algorithms
        // get-counters
        // reset-counters

        private static void VoidCommandRequest(Session session, Command command, params Message[] messages)
        {
            var resultMessages = session.Request(command, messages);
            switch ((resultMessages[0] as KeyValueMessage).Value)
            {
                case "yes":
                    return;
                case "no":
                    throw new System.IO.InvalidDataException((resultMessages[1] as KeyValueMessage).Value);
                default:
                    throw new System.IO.InvalidDataException((resultMessages[1] as KeyValueMessage).Value);
            }
        }

        private static T GetCommandRequest<T>(Session session, Command command, IEnumerable<Message> messages, Func<Message[], T> resultHandler)
        {
            var resultMessages = session.Request(command, messages.ToArray());
            return resultHandler(resultMessages);
        }

        private static T GetCommandRequest<T>(Session session, Command command, Func<Message[], T> resultHandler)
        {
            return GetCommandRequest(session, command, new Message[] { }, resultHandler);
        }

        private static T TypedCommandRequest<T>(Session session, Command command, IEnumerable<Message> messages, Func<Message[], T> resultHandler)
        {
            var resultMessages = session.Request(command, messages.ToArray());
            switch ((resultMessages[0] as KeyValueMessage).Value)
            {
                case "yes":
                    return resultHandler(resultMessages);
                case "no":
                    throw new System.IO.InvalidDataException((resultMessages[1] as KeyValueMessage).Value);
                default:
                    throw new System.IO.InvalidDataException((resultMessages[1] as KeyValueMessage).Value);
            }
        }

        private static T TypedCommandRequest<T>(Session session, Command command, Func<Message[], T> resultHandler)
        {
            return TypedCommandRequest(session, command, new List<Message>(), resultHandler);
        }
    }
}