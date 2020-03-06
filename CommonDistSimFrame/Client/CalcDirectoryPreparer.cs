using System;
using System.IO;
using System.Threading;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Client {
    public class CalcDirectoryPreparer {
        [NotNull] private readonly DistLogger _logger;
        [NotNull] private readonly Settings _settings;
        [NotNull] private readonly ThreadId _threadId;

        public CalcDirectoryPreparer([NotNull] Settings settings, [NotNull] DistLogger logger, [NotNull] ThreadId threadId)
        {
            _settings = settings;
            _logger = logger;
            _threadId = threadId;
        }

        public void Run()
        {
            DirectoryInfo lpgRawDir = new DirectoryInfo(_settings.ClientSettings.LPGRawDirectory);
            DirectoryInfo workingDir = new DirectoryInfo(_settings.ClientSettings.LPGCalcDirectory);
            if (workingDir.Exists) {
                try {
                    workingDir.Delete(true);
                }
                catch (Exception ex) {
                    _logger.Error(ex.Message, _threadId);
                }

                Thread.Sleep(250);
            }

            workingDir.Create();
            FileInfo[] fis = lpgRawDir.GetFiles();
            foreach (FileInfo fi in fis) {
                string dstFullName = Path.Combine(workingDir.FullName, fi.Name);
                _logger.Info("DirectoryPreparer: Copying to " + dstFullName, _threadId);
                fi.CopyTo(dstFullName, true);
            }
        }
    }
}