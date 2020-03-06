using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Automation;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;

namespace CommonDistSimFrame.Client {
    public class CalcExecutor {
        [NotNull] private readonly DistLogger _logger;
        [NotNull] private readonly Settings _mySettings;
        [NotNull] private readonly ThreadId _threadId;

        public CalcExecutor([NotNull] ThreadId threadId, [NotNull] DistLogger logger, [NotNull] Settings mySettings)
        {
            _threadId = threadId;
            _mySettings = mySettings;
            _logger = logger;
        }

        public bool IsWorkingDirFull()
        {
            var di = new DriveInfo(_mySettings.ClientSettings.WorkingDirectory.Substring(0, 1));
            var spaceGb = di.AvailableFreeSpace / 1024.0 / 1024 / 1024;
            _logger.Info(": Free Space on drive " + di.RootDirectory + ": " + spaceGb.ToString("N1", CultureInfo.CurrentCulture) + " GB", _threadId);
            if (spaceGb > 5) {
                return false;
            }

            return true;
        }

        public void Run([NotNull] HouseCreationAndCalculationJob hcj)
        {
            Stopwatch sw = Stopwatch.StartNew();
            const string command = "PHJ -J calcjob.json";
            _logger.Info("Starting calculation process in " + _mySettings.ClientSettings.LPGCalcDirectory, _threadId);
            using (var process = new Process()) {
                var startinfo = new ProcessStartInfo();
                startinfo.Arguments = command;
                startinfo.UseShellExecute = true;
                startinfo.WindowStyle = ProcessWindowStyle.Normal;
                startinfo.FileName = "simulationengine.exe";
                startinfo.WorkingDirectory = _mySettings.ClientSettings.LPGCalcDirectory;
                process.StartInfo = startinfo;
                process.Start();
                while (!process.HasExited) {
                    process.WaitForExit(10000);
                    _logger.Info("still waiting for calculation, duration so far: " + sw.Elapsed, _threadId);
                }
            }

            sw.Stop();
            Thread.Sleep(1000);
            _logger.Info("Finished calculation after " + sw.Elapsed.TotalSeconds + " seconds for " + hcj.Scenario + " - " + hcj.Year + " - " +
                         hcj.House?.Name,
                _threadId);
        }
    }
}