using System.Collections.ObjectModel;
using CommonDistSimFrame.Client;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DistSimClientWpf {
    public class ClientPresenter {
        [NotNull] private readonly MainWindowClient _mainWindow;
        public ClientPresenter([NotNull] ClientThread client, [NotNull] MainWindowClient mainWindow)
        {
            _mainWindow = mainWindow;
            Client = client;
        }
        [NotNull]
        public ClientThread Client { get; }

        [UsedImplicitly]
        [NotNull]
        [ItemNotNull]
        public ObservableCollection<LogMessage> ClientMessages => Client.Logger.LogCol;

        [UsedImplicitly]
        [NotNull]
        public string ClientName => Client.ThreadId.Name;

        [UsedImplicitly]
        public bool RequestNewJobs {
            get => Client.MySettings.RequestNewJobs;
            set {
                Client.MySettings.RequestNewJobs = value;
                foreach (var clientThread in _mainWindow.ClientThreads) {
                    clientThread.MySettings.RequestNewJobs = value;
                }
            }
        }

        [CanBeNull]
        [ItemNotNull]
        public ObservableCollection<LogMessage> LogMessages => Client.Logger.LogCol;

        [NotNull]
        public string Settings => JsonConvert.SerializeObject(Client.MySettings, Formatting.Indented);
    }
}