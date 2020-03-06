using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Common
{
    public class Settings
    {
        [Obsolete("Json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public Settings()
        {
        }

        public Settings([NotNull] ClientSettings clientSettings, [NotNull] ServerSettings serverSettings, [NotNull] string serverIP)
        {
            ClientSettings = clientSettings;
            ServerSettings = serverSettings;
            ServerIP = serverIP;
        }

        public Settings([NotNull] Settings o)
        {
            ServerIP = o.ServerIP;
            ContinueRunning = o.ContinueRunning;
            ClientSettings = new ClientSettings(o.ClientSettings);
            ServerSettings = new ServerSettings(o.ServerSettings);
        }

        [NotNull]
        public ClientSettings ClientSettings { get; set; }
        [NotNull]
        public ServerSettings ServerSettings { get; set; }
        [NotNull]
        public string ServerIP { get; set; }
        public bool ContinueRunning { get; set; } = true;
        public bool RequestNewJobs { get; set; } = true;
    }
}
