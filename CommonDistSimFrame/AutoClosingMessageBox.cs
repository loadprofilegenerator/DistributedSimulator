using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using JetBrains.Annotations;

namespace CommonDistSimFrame {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
#pragma warning disable CA1060 // Move pinvokes to native methods class
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public class AutoClosingMessageBox {
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
#pragma warning restore CA1060 // Move pinvokes to native methods class
#pragma warning disable SA1310 // Field names should not contain underscore
        private const int WM_CLOSE = 0x0010;
#pragma warning restore SA1310 // Field names should not contain underscore
        [NotNull] private readonly string _caption;
        [NotNull] private readonly Timer _timeoutTimer;

        private AutoClosingMessageBox([NotNull] string text, [CanBeNull] string caption, int timeoutnMilliSeconds)
        {
            _caption = caption ?? "Error in DistributedSim";
            _timeoutTimer = new Timer(OnTimerElapsed, null, timeoutnMilliSeconds, Timeout.Infinite);
            MessageBox.Show(text, caption);
        }

        public static void Show([NotNull] string text, [NotNull] string caption, int timeout)
        {
            // ReSharper disable once ObjectCreationAsStatement
#pragma warning disable S1848 // Objects should not be created to be dropped immediately without being used
#pragma warning disable CA1806 // Do not ignore method results
            new AutoClosingMessageBox(text, caption, timeout);
#pragma warning restore CA1806 // Do not ignore method results
#pragma warning restore S1848 // Objects should not be created to be dropped immediately without being used
        }

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("user32.dll", SetLastError = true)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        private static extern IntPtr FindWindow([CanBeNull] string lpClassName, [NotNull] string lpWindowName);

        private void OnTimerElapsed([NotNull] object state)
        {
            IntPtr mbWnd = FindWindow(null, _caption);
            if (mbWnd != IntPtr.Zero) {
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }

            _timeoutTimer.Dispose();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}