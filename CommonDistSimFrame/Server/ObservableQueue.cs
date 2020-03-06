using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Server {
    public class ObservableQueue<T> : INotifyCollectionChanged, IEnumerable<T>, INotifyPropertyChanged {
        [NotNull] [ItemNotNull] private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        [UsedImplicitly]
        public int Count => _queue.Count;

        public bool IsEmpty => _queue.IsEmpty;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Enqueue([NotNull] T item)
        {
            SaveExecuteHelper.Get().SaveExecuteWithWait(() => {
                _queue.Enqueue(item);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
                OnPropertyChanged(nameof(Count));
            });
        }

        public bool TryDequeue([NotNull] out T item)
        {
            var success = _queue.TryDequeue(out var myItem);
            item = myItem;
            if (CollectionChanged != null) {
                SaveExecuteHelper.Get().SaveExecute(() => {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, myItem, 0));
                    OnPropertyChanged(nameof(Count));
                });
            }

            return success;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] [CanBeNull] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}