using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;

namespace DistSimServerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        [NotNull] private readonly ApplicationPresenter _applicationPresenter;
        public MainWindow()
        {
            BindingErrorListener.Listen(m => {
                if (!m.Contains("FindAncestor"))
                {
                    var s = m;
                    var t = new Thread(() => MessageBox.Show(s));
                    Console.WriteLine(m);
                    t.Start();
                }
            });
            InitializeComponent();
            SaveExecuteHelper.Get().Dispatcher = Dispatcher;
            _applicationPresenter = new ApplicationPresenter();
            DataContext = _applicationPresenter;
        }

        private void LstServerMessages_OnMouseDown([NotNull] object sender, [NotNull] MouseButtonEventArgs e)
        {
            MessageBox.Show("hi");
        }

        private void MainWindow_OnClosing([NotNull] object sender, [NotNull] CancelEventArgs e)
        {
            _applicationPresenter.ServerThread.Settings.ContinueRunning = false;
            Thread.Sleep(2500);
            Environment.Exit(0);
        }
    }
}
