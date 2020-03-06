using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Automation;
using CommonDistSimFrame.Common;
using CommonDistSimFrame.Server;
using JetBrains.Annotations;
using MessagePack;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace CommonDistSimFrame.Client {
    public class ClientThread {
        [NotNull] private static readonly object _socketLock = new object();
        [NotNull] private readonly DistLogger _logger;
        [NotNull] private readonly Settings _mySettings;
        [NotNull] private readonly ThreadId _threadId;

        public ClientThread([NotNull] DistLogger logger, [NotNull] Settings mySettings, [NotNull] ThreadId threadId)
        {
            _logger = logger;
            _mySettings = mySettings;
            _threadId = threadId;
            logger.Info("initalizing logger", threadId);
        }

        [NotNull]
        public DistLogger Logger => _logger;

        [NotNull]
        public Settings MySettings => _mySettings;

        [CanBeNull]
        public Exception ThreadException { get; set; }

        [NotNull]
        public ThreadId ThreadId => _threadId;

        public void ExecuteCalcJob([NotNull] MessageFromServerToClient job, [NotNull] CalcExecutor calcExecutor, [NotNull] RequestSocket client)
        {
            var cdp = new CalcDirectoryPreparer(_mySettings, _logger, _threadId);
            cdp.Run();
            HouseCreationAndCalculationJob hcj = null;
            if (!string.IsNullOrWhiteSpace(job.HouseJobStr)) {
                hcj = JsonConvert.DeserializeObject<HouseCreationAndCalculationJob>(job.HouseJobStr);
                if (hcj.CalcSpec == null) {
                    hcj.CalcSpec = JsonCalcSpecification.MakeDefaultsForProduction();
                }

                hcj.CalcSpec.OutputDirectory = "Results";
                string jsonFileName = Path.Combine(_mySettings.ClientSettings.LPGCalcDirectory, "calcjob.json");
                string correctedJob = JsonConvert.SerializeObject(hcj, Formatting.Indented);
                File.WriteAllText(jsonFileName, correctedJob);
                calcExecutor.Run(hcj);
            }
            else {
                _logger.Info("Client #" + _threadId + ": Got a task with an exe, not real, waiting 5s", _threadId);
                Thread.Sleep(5000);
            }

            ReportFinishedCalcJob(job, client, hcj);
        }

        public void ReportDiskFullAndWait([NotNull] RequestSocket client)
        {
            var workingDirErrorMessage = PrepareMessageFromClient(ClientRequestEnum.ReportDiskspaceFull, "no space left on working directory");
            var str = MakeRequest(client, workingDirErrorMessage);
            _logger.Info("Server reply to disk full: " + str.ServerResponse, _threadId);
            Thread.Sleep(5000);
        }

        public bool RequestAndSaveNewLPGFiles([NotNull] RequestSocket client)
        {
            var reqStr = PrepareMessageFromClient(ClientRequestEnum.RequestForLPGFiles, "New files please");
            var answer = MakeRequest(client, reqStr);

            if (!Directory.Exists(_mySettings.ClientSettings.LPGRawDirectory)) {
                Directory.CreateDirectory(_mySettings.ClientSettings.LPGRawDirectory);
            }

            foreach (var jsonfil in answer.LpgFiles) {
                string dstPath = Path.Combine(_mySettings.ClientSettings.LPGRawDirectory, jsonfil.FileName);
                _logger.Info("Writing file " + dstPath + " with length: " + jsonfil.FileLength, _threadId);
                jsonfil.WriteBytesFromJson(dstPath, Logger);
            }

            return true;
        }

        [CanBeNull]
        public MessageFromServerToClient RequestNewJobFromServer([NotNull] RequestSocket client)
        {
            _logger.Info("Client #" + _threadId.Name + ": Asking for work", _threadId);
            var messageToSend = PrepareMessageFromClient(ClientRequestEnum.RequestForJob, "Job Request");
            var answer = MakeRequest(client, messageToSend);
            _logger.Info("Received task: " + answer.OriginalFileName, _threadId);
            return answer;
        }

        public void Run([NotNull] Action<string> msgBoxFunction)
        {
            while (_mySettings.ContinueRunning) {
                try {
                    TryRun();
                }
                catch (Exception ex) {
                    Logger.Error(ex.Message + "\n" + ex.StackTrace, _threadId);
                    msgBoxFunction(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        public void TryRun()
        {
            try {
                _logger.Info("Started the client " + _threadId.Name + " and connecting to " + _mySettings.ServerIP, _threadId);
                using (var client = new RequestSocket()) {
                    client.Connect(_mySettings.ServerIP);
                    _logger.Info("connected to " + _mySettings.ServerIP, _threadId);
                    while (_mySettings.ContinueRunning) {
                        var calcExecutor = new CalcExecutor(_threadId, _logger, _mySettings);
                        while (calcExecutor.IsWorkingDirFull()) {
                            ReportDiskFullAndWait(client);
                            if (!_mySettings.ContinueRunning) {
                                return;
                            }
                        }

                        if (_mySettings.RequestNewJobs) {
                            var job = RequestNewJobFromServer(client);
                            if (job == null) {
                                Thread.Sleep(5000);
                                continue;
                            }

                            // ReSharper disable once SwitchStatementMissingSomeCases
                            switch (job.ServerResponse) {
                                case ServerResponseEnum.NothingToDo:
                                    _logger.Info("Client #" + _threadId.Name + ": Nothing to do, waiting 60s.", _threadId);
                                    Thread.Sleep(60000);
                                    break;
                                case ServerResponseEnum.ServeCalcJob: {
                                    if (!AreAllFilesIdentical(job) && !RequestAndSaveNewLPGFiles(client)) {
                                        _logger.Error("Failed to synchronize the lpg", _threadId);
                                        continue;
                                    }

                                    ExecuteCalcJob(job, calcExecutor, client);
                                    break;
                                }
                                default:
                                    throw new DistSimException("Unknown command");
                            }
                        }
                        else {
                            _logger.Info("Client #" + _threadId.Name + ": Not requesting new jobs, waiting 5s.", _threadId);
                            Thread.Sleep(5000);
                        }
                    }
                }

                _logger.Info("Stopped client " + _threadId.Name, _threadId);
            }
            catch (Exception ex) {
                _logger.Exception(ex, "general failure", _threadId);
                ThreadException = ex;
                throw;
            }
        }

        private bool AreAllFilesIdentical([NotNull] MessageFromServerToClient job)
        {
            if (job.LpgFiles.Count == 0) {
                throw new DistSimException("No lpg files were seen");
            }

            foreach (var file in job.LpgFiles) {
                string dstPath = Path.Combine(_mySettings.ClientSettings.LPGRawDirectory, file.FileName);
                var dstFi = new FileInfo(dstPath);
                if (!dstFi.Exists) {
                    return false;
                }

                if (dstFi.Length != file.FileLength) {
                    return false;
                }
            }

            return true;
        }

        [NotNull]
        private MessageFromServerToClient MakeRequest([NotNull] RequestSocket socket, [NotNull] MessageFromClientToServer message)
        {
            byte[] messageBytes = LZ4MessagePackSerializer.Serialize(message);
            byte[] answer;
            Stopwatch sw = Stopwatch.StartNew();
            TimeSpan ts = new TimeSpan(0, 1, 0);
            lock (_socketLock) {
                var success = socket.TrySendFrame(ts, messageBytes);
                if (!success) {
                    throw new DistSimException("could not send to server");
                }

                success = socket.TryReceiveFrameBytes(ts, out answer);
                if (!success) {
                    throw new DistSimException("could not receive from server");
                }
            }

            var answermessage = LZ4MessagePackSerializer.Deserialize<MessageFromServerToClient>(answer);
            sw.Stop();
            var prettySizeSent = AutomationUtili.MakePrettySize(messageBytes.Length);
            var prettysizeAnswer = AutomationUtili.MakePrettySize(answer.Length);
            _logger.Info("Sent " + prettySizeSent + " and received an answer with a length of " + prettysizeAnswer + ", elapsed: " + sw.Elapsed,
                _threadId);
            return answermessage;
        }

        [NotNull]
        private MessageFromClientToServer PrepareMessageFromClient(ClientRequestEnum status, [NotNull] string message)
        {
            var em = new MessageFromClientToServer(status, _threadId.Name, message, Guid.NewGuid().ToString());
            _logger.Info("Preparing request: " + status + " " + message, _threadId);
            return em;
        }

        private void ReportFinishedCalcJob([NotNull] MessageFromServerToClient job,
                                           [NotNull] RequestSocket client,
                                           [CanBeNull] HouseCreationAndCalculationJob hcj)
        {
            var msg = new MessageFromClientToServer(ClientRequestEnum.ReportFinish, _threadId.Name, "finished calculation", job.TaskGuid);
            if (hcj != null) {
                msg.Scenario = hcj.Scenario;
                msg.Year = hcj.Year;
                msg.Trafokreis = hcj.Trafokreis;
                msg.HouseName = hcj.House?.Name;
            }

            string resultDirectory = Path.Combine(_mySettings.ClientSettings.LPGCalcDirectory, "Results");
            DirectoryInfo di = new DirectoryInfo(resultDirectory);
            if (di.Exists) {
                var files = di.GetFiles("*.*", SearchOption.AllDirectories);
                var filteredFiles = new List<FileInfo>();
                foreach (var file in files) {
                    if (file.Name.ToLower(CultureInfo.InvariantCulture).EndsWith(".dat")) {
                        _logger.Error("Refusing dat file: " + file.FullName, _threadId);
                        continue;
                    }

                    if (file.Length > 100_000_000) {
                        _logger.Error("Refusing too big file: " + file.FullName, _threadId);
                        continue;
                    }

                    filteredFiles.Add(file);
                }

                msg.ResultFiles = MsgFile.ReadMsgFiles(true, filteredFiles, di, _logger, _threadId);
                var reportAnswer = MakeRequest(client, msg);
                _logger.Info("Answer from the finish report:" + reportAnswer.ServerResponse, _threadId);
            }
            else {
                _logger.Info("No output directory created, reporting failure to the server", _threadId);
                var msg2 = new MessageFromClientToServer(ClientRequestEnum.ReportFailure,
                    _threadId.Name,
                    "result directory is missing",
                    job.TaskGuid);
                var reportAnswer = MakeRequest(client, msg2);
                _logger.Info("Answer from the finish report:" + reportAnswer.ServerResponse, _threadId);
            }
        }
    }
}