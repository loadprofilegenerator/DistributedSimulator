using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommonDistSimFrame.Client;
using JetBrains.Annotations;
using MessagePack;

namespace CommonDistSimFrame.Common {
    [MessagePackObject]
    public class MessageFromClientToServer
    {
        public MessageFromClientToServer(ClientRequestEnum clientRequest, [NotNull] string clientName, [NotNull] string message, [NotNull] string taskGuid)
        {
            ClientRequest = clientRequest;
            ClientName = clientName;
            Message = message;
            TaskGuid = taskGuid;
        }
        [Obsolete("Json Only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public MessageFromClientToServer()
        {
        }

        [NotNull]
        [Key(0)]
        public string Message { get; set; }
        [NotNull]
        [Key(1)]
        public string ClientName { get; set; }
        [Key(2)]
        public ClientRequestEnum ClientRequest { get; set; }
        [NotNull]
        [Key(3)]
        public string TaskGuid { get; set; }
        [CanBeNull]
        [ItemNotNull]
        [Key(4)]
        public List<MsgFile> ResultFiles { get; set; }
        [CanBeNull]
        [Key(5)]
        public string Scenario { get; set; }
        [CanBeNull]
        [Key(6)]
        public string Year { get; set; }
        [CanBeNull]
        [Key(7)]
        public string Trafokreis { get; set; }
        [CanBeNull]
        [Key(8)]
        public string HouseName { get; set; }
    }
}