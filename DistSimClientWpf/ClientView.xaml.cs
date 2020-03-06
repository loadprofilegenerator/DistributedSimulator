using System.Windows;
using CommonDistSimFrame;
using JetBrains.Annotations;

namespace DistSimClientWpf {
    /// <summary>
    ///     Interaction logic for ClientView.xaml
    /// </summary>
    public partial class ClientView {
        public ClientView()
        {
            InitializeComponent();
        }

        [CanBeNull]
        private ClientPresenter Presenter => DataContext as ClientPresenter;

        private void BtnStop_OnClick([NotNull] object sender, [NotNull] RoutedEventArgs e)
        {
            if (Presenter == null) {
                throw new DistSimException("Presenter was null");
            }

            Presenter.Client.MySettings.ContinueRunning = false;
        }
    }
}