using System;
using System.Windows.Media;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Common {
    public class LogMessage {
        public LogMessage([NotNull] string message, Severity severity)
        {
            Message = message;
            Severity = severity;
            Time = DateTime.Now;
        }

        [UsedImplicitly]
        [NotNull]
        public Brush BackColor {
            get {
                if (Severity == Severity.Error) {
                    return new SolidColorBrush(Colors.Red);
                }

                if (Severity == Severity.Warning) {
                    return new SolidColorBrush(Colors.Orange);
                }

                if (Severity == Severity.Information) {
                    return new SolidColorBrush(Colors.DeepSkyBlue);
                }

                if (Severity == Severity.Debug) {
                    return new SolidColorBrush(Colors.AntiqueWhite);
                }

                return new SolidColorBrush(Colors.Black);
            }
        }

        [UsedImplicitly]
        [NotNull]
        public string Message { get; set; }

        [UsedImplicitly]
        public Severity Severity { get; set; }

        [UsedImplicitly]
        public DateTime Time { get; set; }
    }
}