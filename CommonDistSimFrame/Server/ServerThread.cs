using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Automation;
using CommonDistSimFrame.Client;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;
using MessagePack;
using NetMQ;
using NetMQ.Sockets;

namespace CommonDistSimFrame.Server {
    public class ServerThread {
        [NotNull] private readonly ClientTracker _clients = new ClientTracker();
        [NotNull] private readonly DistLogger _errorLogger;
        [NotNull] private readonly DistLogger _logger;

        [NotNull] private readonly Settings _settings;

        [NotNull] private readonly ThreadId _threadId;

        public ServerThread([NotNull] Settings settings, [NotNull] ThreadId threadId, [NotNull] DistLogger logger, [NotNull] DistLogger errorLogger)
        {
            _settings = settings;
            _threadId = threadId;
            _logger = logger;
            _errorLogger = errorLogger;
            logger.Info("Initializing Server", _threadId);
        }

        [NotNull]
        [ItemNotNull]
        public ObservableCollection<ServerExecutionTask> ActiveTasks { get; } = new ObservableCollection<ServerExecutionTask>();

        public bool AutomaticRefresh { get; set; } = true;

        [NotNull]
        public ClientTracker Clients => _clients;

        [NotNull]
        [ItemNotNull]
        public ObservableCollection<ServerExecutionTask> FinishedTasks { get; } = new ObservableCollection<ServerExecutionTask>();

        [NotNull]
        public DistLogger Logger => _logger;

        [NotNull]
        [ItemNotNull]
        public ObservableQueue<ServerExecutionTask> OpenTasks { get; } = new ObservableQueue<ServerExecutionTask>();

        [NotNull]
        public Settings Settings => _settings;

        [CanBeNull]
        public Exception ThreadException { get; set; }

        public void RefreshOpenTasks()
        {
            if (_settings.ServerSettings.JsonDirectory == null) {
                throw new DistSimException("Jsondirectory was null");
            }

            if (_settings.ServerSettings.JsonDirectory.Count == 0) {
                throw new DistSimException("Jsondirectory was empty");
            }

            foreach (string singledir in _settings.ServerSettings.JsonDirectory) {
                if (OpenTasks.Count > 1000) {
                    break;
                }

                DirectoryInfo di = new DirectoryInfo(singledir);
                if (!di.Exists) {
                    di.Create();
                    Thread.Sleep(100);
                }
                var files = di.GetFiles("*.json");
                var activeFiles = ActiveTasks.Select(x => x.OriginalJsonFilePath).ToList();
                var openTaskFiles = OpenTasks.Select(x => x.OriginalJsonFilePath).ToList();
                foreach (var fileInfo in files) {
                    if (activeFiles.Contains(fileInfo.FullName)) {
                        _logger.Info("Currently processing " + fileInfo.Name, _threadId);
                        continue;
                    }

                    if (openTaskFiles.Contains(fileInfo.FullName)) {
                        _logger.Info("Currently in queue " + fileInfo.Name, _threadId);
                        continue;
                    }

                    ServerExecutionTask set = new ServerExecutionTask(fileInfo.FullName, fileInfo.Name, Guid.NewGuid().ToString());
                    OpenTasks.Enqueue(set);
                    _logger.Info("Created a job for " + fileInfo.Name, _threadId);
                    if (OpenTasks.Count > 1000) {
                        break;
                    }
                }
            }
        }

        public void Run([NotNull] Action<string> msgBoxFunction)
        {
            while (_settings.ContinueRunning) {
                try {
                    TryRunning();
                }
                catch (Exception ex) {
                    Logger.Error(ex.Message + "\n" + ex.StackTrace, _threadId);
                    msgBoxFunction(ex.Message + "\n" + ex.StackTrace);
                }}
        }

        public void TryRunning()
        {
            try {
                RefreshOpenTasks();
                using (var responseSocket = new ResponseSocket()) {
                    _logger.Info("Started the server", _threadId);
                    responseSocket.Bind(_settings.ServerIP);
                    DateTime lastRefresh = DateTime.Now;
                    while (_settings.ContinueRunning) {
                        if (AutomaticRefresh && (DateTime.Now - lastRefresh).TotalMinutes > 3 && OpenTasks.Count < 500) {
                            lastRefresh = DateTime.Now;
                            RefreshOpenTasks();
                        }

                        try {
                            _logger.Info("Waiting for frame", _threadId);
                            var requestBytes = responseSocket.ReceiveFrameBytes();
                            if (!_settings.ContinueRunning) {
                                _logger.Info("don't continue received, quitting server...", _threadId);
                                return;
                            }

                            string prettySize = AutomationUtili.MakePrettySize(requestBytes.Length);
                            _logger.Info("received a byte[] with a length of " + prettySize, _threadId);
                            MessageFromClientToServer req;
                            try {
                                req = LZ4MessagePackSerializer.Deserialize<MessageFromClientToServer>(requestBytes);
                            }
                            catch (Exception ex) {
                                _logger.Exception(ex, "failed to deserialize string with  " + requestBytes.Length + " bytes", _threadId);
                                AnswerRequest(responseSocket,
                                    new MessageFromServerToClient(ServerResponseEnum.NothingToDo, Guid.NewGuid().ToString()));
                                continue;
                            }

                            _logger.Info("Received: " + req.ClientName + ": " + req.ClientRequest + " ## ", _threadId);
                            MessageFromServerToClient answer;

                            // ReSharper disable once SwitchStatementMissingSomeCases
                            _clients.TrackLastRequest(req);
                            switch (req.ClientRequest) {
                                case ClientRequestEnum.RequestForJob:
                                    answer = HandleRequestForJob(req);
                                    break;
                                case ClientRequestEnum.ReportFinish:
                                    answer = HandleTaskFinishReport(req);
                                    break;
                                case ClientRequestEnum.ReportFailure:
                                    answer = HandleFailureReport(req);
                                    break;
                                case ClientRequestEnum.RequestForLPGFiles:
                                    answer = HandleRequestForLpgFiles(req);
                                    break;
                                case ClientRequestEnum.ReportDiskspaceFull:
                                    answer = HandleDiskFullReport(req);
                                    break;
                                default:
                                    _logger.Error("Invalid client request: " + req.ClientRequest, _threadId);
                                    AnswerRequest(responseSocket,
                                        new MessageFromServerToClient(ServerResponseEnum.NothingToDo, "no idea what you want"));
                                    throw new DistSimException("Invalid request");
                            }

                            _logger.Info("Sent: " + req.ClientName + ": " + answer.ServerResponse, _threadId);
                            AnswerRequest(responseSocket, answer);
                            _clients.TrackLastAnswer(req.ClientName, answer);
                        }
                        catch (Exception ex) {
                            ThreadException = ex;
                            _logger.Exception(ex, "Exception in the server thread inner loop: \n" + ex.StackTrace);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex) {
                ThreadException = ex;
                _logger.Exception(ex, "Exception in the server thread: \n");
                throw;
            }
        }

        private void AnswerRequest([NotNull] ResponseSocket socket, [NotNull] MessageFromServerToClient answer)
        {
            _logger.Info("Sent: " + answer.ServerResponse, _threadId);
            Stopwatch sw = Stopwatch.StartNew();
            byte[] messageBytes = LZ4MessagePackSerializer.Serialize(answer);
            TimeSpan ts = new TimeSpan(0, 1, 0);
            bool success = socket.TrySendFrame(ts, messageBytes);
            if (!success) {
                throw new DistSimException("Failed to transmit answer");
            }

            sw.Stop();
        }

        private static void ArchiveJsonFile([NotNull] ServerExecutionTask set, bool isFailure)
        {
            FileInfo jsonFi = new FileInfo(set.OriginalJsonFilePath ?? throw new DistSimException("Json path was null"));
            string path = "Finished";
            if (isFailure) {
                path = "FailedCalculations";
            }

            string jsonArchive = Path.Combine(jsonFi.DirectoryName ?? throw new DistSimException("no directory"), path);
            DirectoryInfo ja = new DirectoryInfo(jsonArchive);
            if (!ja.Exists) {
                ja.Create();
            }

            string jsonArchiveFn = Path.Combine(ja.FullName, jsonFi.Name);
            set.ArchivedJsonFilePath = jsonArchiveFn;
            FileInfo jsonArchiveFi = new FileInfo(jsonArchiveFn);
            if (jsonArchiveFi.Exists) {
                jsonArchiveFi.Delete();
            }

            jsonFi.MoveTo(jsonArchiveFn);
        }

        [NotNull]
        private MessageFromServerToClient HandleDiskFullReport([NotNull] MessageFromClientToServer req)
        {
            try {
                _logger.Info(req.ClientName + " reports disk full", _threadId);
                return new MessageFromServerToClient(ServerResponseEnum.NothingToDo, Guid.NewGuid().ToString());
            }
            catch (Exception ex) {
                _logger.Exception(ex, "handing disk full report");
                throw;
            }
        }

        [NotNull]
        private MessageFromServerToClient HandleFailureReport([NotNull] MessageFromClientToServer req)
        {
            var task = ActiveTasks.FirstOrDefault(x => x.Guid == req.TaskGuid);
            if (task == null) {
                throw new DistSimException("Invalid guid: " + req.TaskGuid);
            }

            task.FinishStatusMessage = req.ClientRequest + " " + req.Message;
            SaveExecuteHelper.Get().SaveExecuteWithWait(() => ActiveTasks.Remove(task));
            SaveExecuteHelper.Get().SaveExecuteWithWait(() => FinishedTasks.Add(task));
            _errorLogger.Error(task.OriginalJsonFilePath + " - no output at all at client " + req.ClientName, _threadId);
            Logger.Error(task.OriginalJsonFilePath + " - no output at all at client " + req.ClientName, _threadId);
            return new MessageFromServerToClient(ServerResponseEnum.JobFinishAck, req.TaskGuid);
        }

        [NotNull]
        private MessageFromServerToClient HandleRequestForJob([NotNull] MessageFromClientToServer req)
        {
            if (OpenTasks.IsEmpty) {
                return new MessageFromServerToClient(ServerResponseEnum.NothingToDo, req.TaskGuid);
            }

            bool success = OpenTasks.TryDequeue(out var task);
            if (success) {
                SaveExecuteHelper.Get().SaveExecuteWithWait(() => ActiveTasks.Add(task));
                task.Client = req.ClientName;
                task.ExecutionStart = DateTime.Now;
                var answer = new MessageFromServerToClient(ServerResponseEnum.ServeCalcJob, task.Guid);
                answer.HouseJobStr = File.ReadAllText(task.OriginalJsonFilePath ??
                                                      throw new DistSimException("Jsonpath was not found: " + task.OriginalJsonFilePath));
                FileInfo fi = new FileInfo(task.OriginalJsonFilePath);
                answer.OriginalFileName = fi.Name;
                answer.LpgFiles = InitalizeLpgFiles(false);
                return answer;
            }

            return new MessageFromServerToClient(ServerResponseEnum.NothingToDo, req.TaskGuid);
        }

        [NotNull]
        private MessageFromServerToClient HandleRequestForLpgFiles([NotNull] MessageFromClientToServer req)
        {
            _logger.Info("trying to collect lpg files for " + req.ClientName, _threadId);
            try {
                MessageFromServerToClient answer = new MessageFromServerToClient(ServerResponseEnum.ServeLpgFiles, req.TaskGuid);
                answer.LpgFiles = InitalizeLpgFiles(true);
                _logger.Info("sending lpg files to client " + req.ClientName + " total files", _threadId);
                return answer;
            }
            catch (Exception ex) {
                _logger.Exception(ex, "Trying to collect files");
                throw;
            }
        }

        [NotNull]
        private MessageFromServerToClient HandleTaskFinishReport([NotNull] MessageFromClientToServer req)
        {
            try {
                _logger.Info("got a finish report from " + req.ClientName + " for " + req.HouseName, _threadId);
                ServerExecutionTask set = ActiveTasks.FirstOrDefault(x => x.Guid == req.TaskGuid);
                if (set == null) {
                    return new MessageFromServerToClient(ServerResponseEnum.JobFinishAck,req.TaskGuid);
                    //if it is an old task guid from previous run, just ignore the finish report
                    //throw new DistSimException("No task found for guid " + req.TaskGuid);
                }

                set.ExecutionEnd = DateTime.Now;
                SaveExecuteHelper.Get().SaveExecuteWithWait(() => ActiveTasks.Remove(set));
                SaveExecuteHelper.Get().SaveExecuteWithWait(() => {
                    FinishedTasks.Add(set);
                    while (FinishedTasks.Count > 20) {
                        FinishedTasks.RemoveAt(0);
                    }
                });
                set.FinishStatusMessage = req.Message;
                var resultFileArchiveDirectory = MakeResultFileDirectory(req);
                DirectoryInfo di = new DirectoryInfo(resultFileArchiveDirectory);
                if (di.Exists) {
                    _logger.Info("deleting previous results from  " + di.FullName, _threadId);
                    SaveDelete(di);
                    Thread.Sleep(250);
                }

                di.Create();
                _logger.Info("created " + di.FullName, _threadId);
                bool isFailure = false;
                if (req.ResultFiles != null) {
                    foreach (var jsonfile in req.ResultFiles) {
                        string directory = Path.Combine(resultFileArchiveDirectory, jsonfile.RelativeDirectory);
                        DirectoryInfo subdir = new DirectoryInfo(directory);
                        if (!subdir.Exists) {
                            subdir.Create();
                        }

                        string dstPath = Path.Combine(directory, jsonfile.FileName);
                        _logger.Info("writing " + dstPath + "," + "\nresult archive dir: " + resultFileArchiveDirectory + "\n relative dir: " +
                                     jsonfile.RelativeDirectory,
                            _threadId);
                        jsonfile.WriteBytesFromJson(dstPath, Logger);
                        if (jsonfile.FileName.ToLower(CultureInfo.InvariantCulture) == "calculationexceptions.txt") {
                            string fileContent = File.ReadAllText(dstPath);
                            _errorLogger.Error(set.OriginalJsonFilePath + " - error during calc " + fileContent, _threadId);
                            _logger.Error(set.OriginalJsonFilePath + " - error during calc " + fileContent, _threadId);
                            isFailure = true;
                        }
                    }
                }
                else {
                    _logger.Error("No result files were delivered", _threadId);
                }

                ArchiveJsonFile(set, isFailure);
                var answer = new MessageFromServerToClient(ServerResponseEnum.JobFinishAck, req.TaskGuid);
                return answer;
            }
            catch (Exception ex) {
                _logger.Exception(ex, "Error while handling a finish report  " + ex.Message);
                throw;
            }
        }

        [NotNull]
        [ItemNotNull]
        private List<MsgFile> InitalizeLpgFiles(bool addFullFiles)
        {
            var lpgDir = new DirectoryInfo(_settings.ServerSettings.LPGStorageDirectory);
            var files = lpgDir.GetFiles("*.*");
            if (files.Length == 0) {
                throw new DistSimException("No files found");
            }

            var filteredFiles = new List<FileInfo>();
            foreach (var fi in files) {
                if (fi.Name.StartsWith("LPG") && fi.Name.EndsWith(".zip")) {
                    continue;
                }

                if (fi.Name.StartsWith("Setup") && fi.Name.EndsWith(".exe")) {
                    continue;
                }

                filteredFiles.Add(fi);
            }

            var lpgFiles = MsgFile.ReadMsgFiles(addFullFiles, filteredFiles, lpgDir, _logger, _threadId);
            return lpgFiles;
        }

        [NotNull]
        private string MakeResultFileDirectory([NotNull] MessageFromClientToServer req)
        {
            if (_settings.ServerSettings.ResultArchiveDirectory == null) {
                throw new DistSimException("Result archive dir is null");
            }

            string resultFileArchiveDirectory = _settings.ServerSettings.ResultArchiveDirectory;
            bool allok = true;
            if (!string.IsNullOrWhiteSpace(req.Scenario)) {
                resultFileArchiveDirectory = Path.Combine(resultFileArchiveDirectory, AutomationUtili.CleanFileName(req.Scenario));
            }
            else {
                allok = false;
            }

            if (!string.IsNullOrWhiteSpace(req.Year)) {
                resultFileArchiveDirectory = Path.Combine(resultFileArchiveDirectory, AutomationUtili.CleanFileName(req.Year));
            }
            else {
                allok = false;
            }

            if (!string.IsNullOrWhiteSpace(req.Trafokreis)) {
                resultFileArchiveDirectory = Path.Combine(resultFileArchiveDirectory, AutomationUtili.CleanFileName(req.Trafokreis));
            }
            else {
                allok = false;
            }

            if (!string.IsNullOrWhiteSpace(req.HouseName)) {
                resultFileArchiveDirectory = Path.Combine(resultFileArchiveDirectory, AutomationUtili.CleanFileName(req.HouseName));
            }
            else {
                allok = false;
            }

            if (!allok) {
                var dt = DateTime.Now;
                string datetimestr = dt.Year + "." + dt.Month + "." + dt.Day + "-" + dt.Hour + "." + dt.Minute + "." + dt.Second;
                resultFileArchiveDirectory = Path.Combine(resultFileArchiveDirectory, "Broken", datetimestr);
                Logger.Error("scenario information was broken, using the following directory instead: " + resultFileArchiveDirectory, _threadId);
            }

            Logger.Info("Saving results to: " + resultFileArchiveDirectory, _threadId);
            if (resultFileArchiveDirectory == _settings.ServerSettings.ResultArchiveDirectory) {
                throw new DistSimException("Somehow the resultdirectory was the main archive directory. this is wrong.");
            }

            Logger.Info("Make result archive directory " + resultFileArchiveDirectory, _threadId);
            return resultFileArchiveDirectory;
        }

        private void SaveDelete([NotNull] DirectoryInfo di)
        {
            var files = di.GetFiles("*.*", SearchOption.AllDirectories);
            if (files.Length > 250) {
                throw new DistSimException("Trying to delete too many files: " + files.Length);
            }

            foreach (var fileInfo in files) {
                try {
                    fileInfo.Delete();
                }
                catch (Exception ex) {
                    Logger.Exception(ex, "Failed to delete file " + fileInfo.FullName);
                }
            }

            var subdirs = di.GetDirectories();
            foreach (var subdir in subdirs) {
                try {
                    subdir.Delete(true);
                }
                catch (Exception ex) {
                    Logger.Exception(ex, "Failed to delete file " + subdir.FullName);
                }
            }

            di.Delete(true);
        }
    }
}