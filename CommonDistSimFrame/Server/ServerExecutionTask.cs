using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Server {
    public class ServerExecutionTask : INotifyPropertyChanged {
        [CanBeNull] private string _client;
        private DateTime _executionEnd;
        private DateTime _executionStart;
        [CanBeNull] private string _finishStatusMessage;

        public ServerExecutionTask([NotNull] string originalJsonPath, [NotNull] string taskName, [NotNull] string guid)
        {
            OriginalJsonFilePath = originalJsonPath;
            TaskName = taskName;
            Guid = guid;
            CreationTime = DateTime.Now;
        }

        [CanBeNull]
        public string ArchivedJsonFilePath { get; set; }

        [CanBeNull]
        public string Client {
            get => _client;
            set {
                if (value == _client) {
                    return;
                }

                _client = value;
                OnPropertyChanged(nameof(Client));
            }
        }

        [UsedImplicitly]
        public DateTime CreationTime { get; }

        [UsedImplicitly]
        public TimeSpan Duration {
            get {
                var ts = ExecutionEnd - ExecutionStart;
                return ts;
            }
        }

        public DateTime ExecutionEnd {
            get => _executionEnd;
            set {
                if (value.Equals(_executionEnd)) {
                    return;
                }

                _executionEnd = value;
                OnPropertyChanged(nameof(ExecutionEnd));
                OnPropertyChanged(nameof(Duration));
            }
        }

        [UsedImplicitly]
        public DateTime ExecutionStart {
            get => _executionStart;
            set {
                if (value.Equals(_executionStart)) {
                    return;
                }

                _executionStart = value;
                OnPropertyChanged(nameof(ExecutionStart));
                OnPropertyChanged(nameof(Duration));
            }
        }

        [UsedImplicitly]
        [CanBeNull]
        public string FinishStatusMessage {
            get => _finishStatusMessage;
            set {
                if (value == _finishStatusMessage) {
                    return;
                }

                _finishStatusMessage = value;
                OnPropertyChanged(nameof(FinishStatusMessage));
            }
        }

        [NotNull]
        public string Guid { get; }

        [CanBeNull]
        public string OriginalJsonFilePath { get; set; }

        [NotNull]
        public string TaskName { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([NotNull] string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}