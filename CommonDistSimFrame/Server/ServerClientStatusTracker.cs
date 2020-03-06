using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Server
{
    public class ServerClientStatusTracker : INotifyPropertyChanged
    {
        [CanBeNull] private string _lastRequest;
        private DateTime _lastRequestTime;
        [CanBeNull] private string _lastTask;

        public ServerClientStatusTracker([NotNull] string clientName)
        {
            ClientName = clientName;
        }

        [NotNull]
        public string ClientName { get; }

        [UsedImplicitly]
        public DateTime LastRequestTime
        {
            get => _lastRequestTime;
            set
            {
                if (value.Equals(_lastRequestTime)) {
                    return;
                }

                _lastRequestTime = value;
                OnPropertyChanged(nameof(LastRequestTime));
            }
        }

        private int _completedJobs;
        [UsedImplicitly]
        public int CompletedJobs
        {
            get => _completedJobs;
            set
            {
                if (value.Equals(_completedJobs))
                {
                    return;
                }

                _completedJobs = value;
                OnPropertyChanged(nameof(CompletedJobs));
            }
        }
        private int _failedJobs;
        [UsedImplicitly]
        public int FailedJobs
        {
            get => _failedJobs;
            set
            {
                if (value.Equals(_failedJobs))
                {
                    return;
                }

                _failedJobs = value;
                OnPropertyChanged(nameof(FailedJobs));
            }
        }
        [UsedImplicitly]
        [CanBeNull]
        public string LastRequest
        {
            get => _lastRequest;
            set
            {
                if (value == _lastRequest) {
                    return;
                }

                _lastRequest = value;
                OnPropertyChanged(nameof(LastRequest));
            }
        }
        [UsedImplicitly]
        [CanBeNull]
        public string LastTask
        {
            get => _lastTask;
            set
            {
                if (value == _lastTask) {
                    return;
                }

                _lastTask = value;
                OnPropertyChanged(nameof(LastTask));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([NotNull] string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}