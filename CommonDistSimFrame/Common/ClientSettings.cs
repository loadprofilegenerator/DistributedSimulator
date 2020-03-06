using System;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CommonDistSimFrame.Common {
    public class ClientSettings {
        public ClientSettings([NotNull] ClientSettings o)
        {
            WorkingDirectory = o.WorkingDirectory;
            NumberOfThreads = o.NumberOfThreads;
        }

        public ClientSettings([NotNull] string workingDirectory, int numberOfThreads)
        {
            WorkingDirectory = workingDirectory;
            NumberOfThreads = numberOfThreads;
        }

        // ReSharper disable once NotNullMemberIsNotInitialized
        [Obsolete("JsonOnly")]
        public ClientSettings()
        {
        }

        [NotNull]
        public string WorkingDirectory { get; set; }

        [NotNull][JsonIgnore]
        public string LPGRawDirectory => Path.Combine(WorkingDirectory, "LPG");

        [NotNull]
        [JsonIgnore]
        public string LPGCalcDirectory => Path.Combine(WorkingDirectory, "LPGCalc");

        public int NumberOfThreads { get; set; }
    }
}