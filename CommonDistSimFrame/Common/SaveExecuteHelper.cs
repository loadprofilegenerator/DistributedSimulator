using System;
using System.Threading;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Common {
    public class SaveExecuteHelper {
        [NotNull] private static readonly SaveExecuteHelper _self = new SaveExecuteHelper();

        private SaveExecuteHelper()
        {
        }

        [CanBeNull]
        public Dispatcher Dispatcher { get; set; }

        [NotNull]
        public static SaveExecuteHelper Get() => _self;

        public void SaveExecute([NotNull] Action action)
        {
            if (Dispatcher != null && Thread.CurrentThread != Dispatcher.Thread) {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }
            else {
                action();
            }
        }

        public void SaveExecuteWithWait([NotNull] Action action)
        {
            if (Dispatcher != null && Thread.CurrentThread != Dispatcher.Thread) {
                var finished = false;
                Action b = () => {
                    action();
                    finished = true;
                };
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, b);
                while (!finished) {
                    Thread.Sleep(1);
                }
            }
            else {
                action();
            }
        }
    }
}