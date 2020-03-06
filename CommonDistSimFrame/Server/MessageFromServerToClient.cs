using System;
using System.Collections.Generic;
using CommonDistSimFrame.Client;
using JetBrains.Annotations;
using MessagePack;

namespace CommonDistSimFrame.Server {
    [MessagePackObject]
    public class MessageFromServerToClient {
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once NotNullMemberIsNotInitialized
        [Obsolete("Json only")]
        public MessageFromServerToClient()
        {
        }

        public MessageFromServerToClient(ServerResponseEnum serverResponse, [NotNull] string taskGuid)
        {
            ServerResponse = serverResponse;
            TaskGuid = taskGuid;
        }

        [CanBeNull]
        [Key(0)]
        public string HouseJobStr { get; set; }

        [NotNull][ItemNotNull]
        [Key(1)]
        public List<MsgFile> LpgFiles { get; set; } = new List<MsgFile>();
        [CanBeNull]
        [Key(2)]
        public string OriginalFileName { get; set; }
        [Key(3)]
        public ServerResponseEnum ServerResponse { get; set; }
        [NotNull]
        [Key(4)]
        public string TaskGuid { get; set; }
    }
}