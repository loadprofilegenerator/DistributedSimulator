using JetBrains.Annotations;

namespace CommonDistSimFrame.Client {
    public class ThreadId {
        public ThreadId([NotNull] string computerName, int computerIdx)
        {
            ComputerName = computerName;
            ComputerIdx = computerIdx;
        }

        public int ComputerIdx { get; set; }

        [NotNull]
        public string ComputerName { get; set; }

        [NotNull]
        public string Name => ComputerName + " - " + ComputerIdx;
    }
}