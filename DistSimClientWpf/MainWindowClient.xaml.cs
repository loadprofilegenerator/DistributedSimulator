using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CommonDistSimFrame;
using CommonDistSimFrame.Client;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;

namespace DistSimClientWpf {
    /// <summary>
    ///     Interaction logic for MainWindowClient
    /// </summary>
    public partial class MainWindowClient {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        [NotNull] private readonly ApplicationPresenter _applicationPresenter;
        [NotNull] [ItemNotNull] private readonly List<ClientThread> _clientThreads = new List<ClientThread>();
        [NotNull] [ItemNotNull] private readonly List<Thread> _threads = new List<Thread>();

        public MainWindowClient()
        {
            BindingErrorListener.Listen(m => {
                if (!m.Contains("FindAncestor")) {
                    var s = m;
                    var t = new Thread(() => MessageBox.Show(s));
                    Console.WriteLine(m);
                    t.Start();
                }
            });
            InitializeComponent();
            _applicationPresenter = new ApplicationPresenter();
            SaveExecuteHelper.Get().Dispatcher = Dispatcher;
            for (int i = 1; i <= _applicationPresenter.Settings.ClientSettings.NumberOfThreads; i++) {
                string workingDir = _applicationPresenter.Settings.ClientSettings.WorkingDirectory;
#pragma warning disable CA2000 // Dispose objects before losing scope
                string logfilename = Path.Combine(workingDir, "DistCalc.Client." + i + ".Log.sqlite");
                DistLogger logger = new DistLogger(logfilename, "Log", null);
#pragma warning restore CA2000 // Dispose objects before losing scope
                Settings cs = new Settings(_applicationPresenter.Settings);
                cs.ClientSettings.WorkingDirectory = Path.Combine(cs.ClientSettings.WorkingDirectory, "c" + i);
                ClientThread ct = new ClientThread(logger, cs, new ThreadId(Environment.MachineName, i));
                _clientThreads.Add(ct);
                var t = new Thread(() => ct.Run(ShowMessageBox));
                _threads.Add(t);
                t.Start();
                TabItem ti = new TabItem();
                ti.Header = "Client " + i;
                ClientView cv = new ClientView();
                ClientPresenter cp = new ClientPresenter(ct, this);
                cv.DataContext = cp;
                ti.Content = cv;
                MyTabControl.Items.Add(ti);
            }
        }

        [NotNull]
        [ItemNotNull]
        public List<ClientThread> ClientThreads => _clientThreads;

        private void MainWindowClient_OnClosing([NotNull] object sender, [NotNull] CancelEventArgs e)
        {
            foreach (var clientThread in _clientThreads) {
                clientThread.MySettings.ContinueRunning = false;
            }

            foreach (var thread in _threads) {
                thread.Abort();
            }

            Environment.Exit(0);
        }

        [NotNull] private readonly Random _rnd = new Random();
        private void ShowMessageBox([NotNull] string s)
        {
            AutoClosingMessageBox.Show(s, "Error in the Distributed Simulator", 30000 + (_rnd.Next(30) * 1000));
        }
    }
}