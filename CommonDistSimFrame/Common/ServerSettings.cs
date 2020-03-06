using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Common {
    public class ServerSettings {
        public ServerSettings([NotNull] ServerSettings o)
        {
            LPGStorageDirectory = o.LPGStorageDirectory;
            JsonDirectory = o.JsonDirectory;
            ResultArchiveDirectory = o.ResultArchiveDirectory;
        }

        [Obsolete("json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public ServerSettings()
        {
        }

        public ServerSettings([NotNull] string lpgStorageDirectory, [NotNull][ItemNotNull] List<string> jsonDirectory, [NotNull] string resultArchiveDirectory)
        {
            LPGStorageDirectory = lpgStorageDirectory;
            JsonDirectory = jsonDirectory;
            ResultArchiveDirectory = resultArchiveDirectory;
        }

        [NotNull]
        public string LPGStorageDirectory { get; set; }
        [NotNull]
        [ItemNotNull]
        public List<string> JsonDirectory { get; set; } = new List<string>();
        [NotNull]
        public string ResultArchiveDirectory { get; set; }
    }
}