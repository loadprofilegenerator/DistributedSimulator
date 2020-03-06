using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace CommonDistSimTests
{
    public class Deploy
    {
        [NotNull] private readonly ITestOutputHelper _h;

        private void Info([NotNull] string s)
        {
            _h.WriteLine(s);
        }
        public Deploy([NotNull] ITestOutputHelper h)
        {
            _h = h;
        }

        [Fact]
        public void DeployDistributedSimNewServerConfig()
        {
            var dstPath = @"\\bfh-lpg-server\Fla\DistSimServer";
            const string lpgDstDir = @"\\bfh-lpg-server\Fla\LPG";
            WriteConfig(dstPath, 5, lpgDstDir, "c:\\work\\distSimTmp");

        }

        [Fact]
        public void DeployDistributedSim()
        {
            // client
            const string lpgDstDir = @"\\bfh-lpg-server\Fla\LPG";

            // server files
            var dstPathSvr = @"\\bfh-lpg-server\Fla\DistSimServer";
            CopyRec(@"V:\Dropbox\DistributedSimulator\DistSimServerWpf\bin\Debug", dstPathSvr, true);
            CopyRec(@"V:\Dropbox\LPGReleases\releases9.0.0", lpgDstDir, true);
            WriteConfig(dstPathSvr, 5, lpgDstDir, "c:\\work\\distSimTmp");
            return;
            // old server
            var dstPath = @"\\bfh-lpg-server\Fla\DistSimClient";
            CopyRec(@"V:\Dropbox\DistributedSimulator\DistSimClientWpf\bin\Debug", dstPath, true);
            WriteConfig(dstPath, 15, lpgDstDir, "f:\\work\\distSimTmp");

            // ws
            dstPath = @"\\jlco-lpg-ws\flaarchive\DistSimClient";
            CopyRec(@"V:\Dropbox\DistributedSimulator\DistSimClientWpf\bin\Debug", dstPath, true);
            WriteConfig(dstPath, 7, lpgDstDir, "c:\\work\\distSimTmp");

            // calc server
            dstPath = @"\\147.87.96.180\work\DistSimClient";
            CopyRec(@"V:\Dropbox\DistributedSimulator\DistSimClientWpf\bin\Debug", dstPath, true);
            WriteConfig(dstPath, 30, lpgDstDir, "c:\\work\\distSimTmp");
            return;
            // student pcs
            dstPath = @"u:\DistSimClient-4";
            CopyRec(@"V:\Dropbox\DistributedSimulator\DistSimClientWpf\bin\Debug", dstPath, true);
            WriteConfig(dstPath, 4, lpgDstDir, "c:\\temp\\distSimTmp");

        }

        private void WriteConfig([NotNull] string dstPath, int threads, [NotNull] string lpgDir, [NotNull] string tmpDir)
        {
            ClientSettings clientSettings = new ClientSettings(tmpDir, threads);
            List<string> directoriesToUse = new List<string>();
            directoriesToUse.Add(@"X:\HouseJobs\Blockstrom\DirectHouseholds");
            directoriesToUse.Add(@"X:\HouseJobs\Blockstrom\TemplatedHouseholds");
            directoriesToUse.Add(@"X:\HouseJobs\Blockstrom\TemplatedHouses");
            directoriesToUse.Add(@"X:\HouseJobs\Present\2017\Districts");
            DirectoriesForOneScenario(directoriesToUse, "Utopia");
            DirectoriesForOneScenario(directoriesToUse, "Nep");
            DirectoriesForOneScenario(directoriesToUse, "Pom");
            DirectoriesForOneScenario(directoriesToUse, "PomSmart");
            DirectoriesForOneScenario(directoriesToUse, "Dystopia");
            ServerSettings serverSettings = new ServerSettings(lpgDir, directoriesToUse, @"X:\DS");
            //TODO: make this configureable
            const string serverip = "tcp://xxx.xxx.xxx.xxx:81";
            Settings settings = new Settings(clientSettings, serverSettings, serverip);
            string dstfn = Path.Combine(dstPath, "ClientSettings.json");
            File.WriteAllText(dstfn, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static void DirectoriesForOneScenario([NotNull] List<string> directoriesToUse, string scenario)
        {
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2020\Districts");
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2025\Districts");
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2030\Districts");
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2035\Districts");
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2040\Districts");
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2045\Districts");
            directoriesToUse.Add(@"X:\HouseJobs\" + scenario + @"\2050\Districts");
        }

        private void CopyRec([NotNull] string src, [NotNull] string dst,  bool deleteExtraDirectories)
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
            Info("Copying from " + src + " to " + dst);
            DirectoryInfo srcInfo = new DirectoryInfo(src);
            DirectoryInfo dstInfo = new DirectoryInfo(dst);
            int filecount = 0;
            long fileSize = 0;
            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }
            CopyFilesRecursively(srcInfo, dstInfo, ref filecount, ref fileSize,  deleteExtraDirectories);
            Info("Copied " + filecount + " with a total of " + Automation.AutomationUtili.MakePrettySize(fileSize));
        }
        private void CopyFilesRecursively([NotNull] DirectoryInfo source, [NotNull] DirectoryInfo target, ref int filecount,
                                                 ref long filesize,  bool deleteExtraDirectories)
        {
            var targetDirs = target.GetDirectories().ToList();
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), ref filecount, ref filesize,  deleteExtraDirectories);
                var matchingTargetDir = targetDirs.FirstOrDefault(x => x.Name == dir.Name);
                if (matchingTargetDir != null)
                {
                    targetDirs.Remove(matchingTargetDir);
                }
            }

            foreach (DirectoryInfo extraDir in targetDirs)
            {
                if (deleteExtraDirectories)
                {
                    Info("Extra directory " + extraDir.FullName + ", deleting");
                    extraDir.Delete(true);
                }
                else
                {
                    Info("Extra directory " + extraDir.FullName + ", ignoring");
                }
            }

            var targetFiles = target.GetFiles().ToList();
            foreach (FileInfo file in source.GetFiles())
            {
                string targetpath = Path.Combine(target.FullName, file.Name);
                if (!File.Exists(targetpath))
                {
                    file.CopyTo(targetpath);
                    filecount++;
                    filesize += file.Length;
                }
                else
                {
                    FileInfo targetInfo = new FileInfo(targetpath);
                    var matchingTargetFile = targetFiles.FirstOrDefault(x => x.Name == file.Name);
                    if (matchingTargetFile != null)
                    {
                        targetFiles.Remove(matchingTargetFile);
                    }

                    if (IsFileChanged(file, targetInfo))
                    {
                        Info("File changed: " + file);
                        file.CopyTo(targetpath, true);
                        filecount++;
                        filesize += file.Length;
                    }
                }
            }

            foreach (FileInfo info in targetFiles)
            {
                Info("Deleted " + info.Name);
                info.Delete();
            }
        }

        private static bool IsFileChanged([NotNull] FileInfo src, [NotNull] FileInfo dst)
        {
            if (src.Length != dst.Length)
            {
                return true;
            }

            if (src.LastWriteTime != dst.LastWriteTime)
            {
                return true;
            }

            return false;
        }
    }
}
