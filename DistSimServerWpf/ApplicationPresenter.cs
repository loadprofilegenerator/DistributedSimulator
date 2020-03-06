using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using CommonDistSimFrame;
using CommonDistSimFrame.Client;
using CommonDistSimFrame.Common;
using CommonDistSimFrame.Server;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DistSimServerWpf {
    public class ApplicationPresenter : INotifyPropertyChanged {
        public ApplicationPresenter()
        {
            var settings = LoadSettings();
#pragma warning disable CA2000 // Dispose objects before losing scope
            DistLogger logger = new DistLogger(@"c:\work\DistCalc.Server.Log.sqlite", "Log", null);
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning disable CA2000 // Dispose objects before losing scope
            DistLogger errorlogger = new DistLogger(@"c:\work\DistCalc.Server.CalcErrors.sqlite", "Log", null);
#pragma warning restore CA2000 // Dispose objects before losing scope
            ServerThread = new ServerThread(settings, new ThreadId("Server", 1), logger, errorlogger);
            //AutoClosingMessageBox.Show("Starting the server", "ServerStart", 5000);
            var t = new Thread(() => ServerThread.Run(ShowMessageBox));
            t.Start();
        }

        [CanBeNull]
        [ItemNotNull]
        public ObservableCollection<LogMessage> LogMessages => ServerThread.Logger.LogCol;

        [CanBeNull]
        [ItemNotNull]
        public ObservableCollection<LogMessage> ErrorMessages => ServerThread.Logger.ErrorCol;

        [NotNull]
        public ServerThread ServerThread { get; set; }

        [NotNull]
        public string Settings => JsonConvert.SerializeObject(ServerThread.Settings, Formatting.Indented);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] [CanBeNull] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        [NotNull]
        private static Settings LoadSettings()
        {
            FileInfo dst = new FileInfo("clientsettings.json");
            if (dst.Exists) {
                string str = File.ReadAllText(dst.FullName);
                return JsonConvert.DeserializeObject<Settings>(str);
            }

#pragma warning disable S1075 // URIs should not be hardcoded
            dst = new FileInfo("c:\\work\\clientsettings.json");
#pragma warning restore S1075 // URIs should not be hardcoded
            if (dst.Exists) {
                string str = File.ReadAllText(dst.FullName);
                return JsonConvert.DeserializeObject<Settings>(str);
            }

            var settings = new Settings(new ClientSettings("workingdir", 1),
                new ServerSettings("lpgstordir", new List<string>(), "archivedir"),
                "serverip");
            File.WriteAllText(dst.FullName, JsonConvert.SerializeObject(settings, Formatting.Indented));
            throw new DistSimException("Could not find config, new one written to " + dst.FullName);
        }

        [NotNull] private readonly Random _rnd = new Random();

        private void ShowMessageBox([NotNull] string s)
        {
            AutoClosingMessageBox.Show(s, "Error in the Distributed Simulator", 15000 + (_rnd.Next(15) * 1000));
        }
    }
}