using System;
using System.Collections.Generic;
using System.IO;
using Automation;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;
using MessagePack;

namespace CommonDistSimFrame.Client {
    [MessagePackObject]
    public class MsgFile {
        public MsgFile([NotNull] string fileName, [NotNull] string relativeDirectory, long fileLength, DateTime fileModifiedDate)
        {
            FileName = fileName;
            RelativeDirectory = relativeDirectory;
            FileLength = fileLength;
            FileModifiedDate = fileModifiedDate;
        }

        public MsgFile([NotNull] FileInfo fi, [NotNull] string relativeDirectory)
        {
            FileName = fi.Name;
            RelativeDirectory = relativeDirectory;
            FileLength = fi.Length;
            FileModifiedDate = fi.LastWriteTime;
        }

        // ReSharper disable once NotNullMemberIsNotInitialized
        [Obsolete("json only")]
        public MsgFile()
        {
        }

        [CanBeNull]
        [Key(0)]
#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] FileContent{ get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [Key(1)]
        public long FileLength { get; set; }
        [Key(2)]
        public DateTime FileModifiedDate { get; set; }

        [NotNull]
        [Key(3)]
        public string FileName { get; set; }

        [NotNull]
        [Key(4)]
        public string RelativeDirectory { get; set; }

        [NotNull]
        [ItemNotNull]
        public static List<MsgFile> ReadMsgFiles(bool addFullFiles,
                                                  [NotNull] [ItemNotNull] List<FileInfo> filteredFiles,
                                                  [NotNull] DirectoryInfo baseDir, [NotNull] DistLogger logger, [NotNull] ThreadId threadId)
        {
            var lpgFiles = new List<MsgFile>();
            long totalSize = 0;
            foreach (var fi in filteredFiles) {
                var relativeDirectory = "";
                if (fi.DirectoryName?.Length > baseDir.FullName.Length) {
                    relativeDirectory = fi.DirectoryName.Substring(baseDir.FullName.Length).Trim().Trim('\\');
                }

                MsgFile jf = new MsgFile(fi, relativeDirectory);
                if (addFullFiles) {
                    jf.FileContent = File.ReadAllBytes(fi.FullName);
                }

                totalSize += fi.Length;
                lpgFiles.Add(jf);
            }

            string prettySize = AutomationUtili.MakePrettySize(totalSize);
            logger.Info("Collected files with " + prettySize, threadId);
            return lpgFiles;
        }

        public void WriteBytesFromJson([NotNull] string dstPath, [NotNull] DistLogger logger)
        {
            if (FileContent == null) {
                throw new DistSimException("Tried to save an empty jsonfile");
            }

            if (FileContent.Length != FileLength) {
                throw new DistSimException("File length is wrong");
            }

            try {
                File.WriteAllBytes(dstPath, FileContent);
                File.SetLastWriteTime(dstPath, FileModifiedDate);
            }
            catch (Exception ex) {
                logger.Exception(ex, "failed to write results to " + dstPath);
                throw;
            }
        }
    }
}