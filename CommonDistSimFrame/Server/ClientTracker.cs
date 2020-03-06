using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Server {
    public class ClientTracker {
        [NotNull]
        [ItemNotNull]
        public ObservableCollection<ServerClientStatusTracker> ClientStatus { get; } = new ObservableCollection<ServerClientStatusTracker>();

        public static string CleanClientName(string s)
        {
            if (!s.Contains(" ")) {
                throw new DistSimException("invalid client name");
            }

            return s.Substring(0, s.IndexOf(" ")).Trim();
        }
        public void TrackLastAnswer([NotNull] string reqClientName, [NotNull] MessageFromServerToClient answer)
        {
            string cleanname = CleanClientName(reqClientName);
            var client = ClientStatus.FirstOrDefault(x => x.ClientName == cleanname);
            if (client == null) {
                client = new ServerClientStatusTracker(cleanname);
                SaveExecuteHelper.Get().SaveExecuteWithWait(() => ClientStatus.Add(client));
            }

            client.LastRequestTime = DateTime.Now;
            client.LastTask = answer.ServerResponse.ToString();
        }

        public void TrackLastRequest([NotNull] MessageFromClientToServer req)
        {
            string cleanname = CleanClientName(req.ClientName);
            var client = ClientStatus.FirstOrDefault(x => x.ClientName == cleanname);
            if (client == null) {
                client = new ServerClientStatusTracker(cleanname);
                SaveExecuteHelper.Get().SaveExecuteWithWait(() => ClientStatus.Add(client));
            }

            if (req.ClientRequest == ClientRequestEnum.ReportFinish) {
                client.CompletedJobs++;
            }

            if (req.ClientRequest == ClientRequestEnum.ReportFailure) {
                client.FailedJobs++;
            }
            client.LastRequestTime = DateTime.Now;
            client.LastRequest = req.ClientRequest + " " + req.Message;
        }
    }
}