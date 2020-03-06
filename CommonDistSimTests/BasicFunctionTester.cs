using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using Autofac;
using Automation;
using CommonDistSimFrame.Client;
using CommonDistSimFrame.Common;
using CommonDistSimFrame.Server;
using JetBrains.Annotations;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace CommonDistSimTests {
    public class BasicFunctionTester {
        public BasicFunctionTester([NotNull] ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [NotNull] private readonly ITestOutputHelper _testOutputHelper;

        [NotNull]
        private ServerThread StartServerThread([NotNull] IContainer container)
        {
            var server = container.Resolve<ServerThread>(new TypedParameter(typeof(ThreadId), new ThreadId("Server", 1)));
            Thread t = new Thread(() => server.Run(ShowMessageBox));
            _testOutputHelper.WriteLine("starting server thread");
            t.Start();
            return server;
        }

        private static void ShowMessageBox([NotNull] string s)
        {
            MessageBox.Show(s, "error while testing");
        }
        [NotNull]
        private Settings PrepareTestEnviroment([NotNull] string tmpDir, [NotNull] out ContainerBuilder builder)
        {
            var settings = MakeTestSettings(tmpDir);

            // basic directory
            try {
                if (Directory.Exists(tmpDir)) {
                    Directory.Delete(tmpDir, true);
                }
            }
            catch (Exception ex) {
                _testOutputHelper.WriteLine(ex.Message);
            }

            // dummy job
            if (!Directory.Exists(settings.ServerSettings.JsonDirectory[0])) {
                Directory.CreateDirectory(settings.ServerSettings.JsonDirectory[0]);
            }

            var hcc = new HouseCreationAndCalculationJob("Utopia", "2020", "trafokreis");
            hcc.House = new HouseData("houseguid", "HT01", 10000, 10000, "myhousename");

            hcc.House.Households.Add(new HouseholdData("householdguid", 1000, ElectricCarUse.NoElectricCar, "myhh", null, null, null, null, HouseholdDataSpecifictionType.ByPersons));
            hcc.House.Households[0].HouseholdDataPersonSpecification = new HouseholdDataPersonSpecification(new List<PersonData> {
                new PersonData(30, Gender.Male), });
            hcc.CalcSpec = JsonCalcSpecification.MakeDefaultsForProduction();
            hcc.CalcSpec.OutputDirectory = "Results";
            JsonReference geoloc = new JsonReference("(Germany) Chemnitz", "eddeb22c-fbd4-44c1-bf2d-fbde3342f1bd");
            JsonReference tempReference = new JsonReference("Berlin, Germany 1996 from Deutscher Wetterdienst DWD (www.dwd.de)",
                "ec337ba6-60a1-404b-9db0-9be52c9e5702");
            hcc.CalcSpec.TemperatureProfile = tempReference;
            hcc.CalcSpec.GeographicLocation = geoloc;
            hcc.CalcSpec.DefaultForOutputFiles = OutputFileDefault.None;
            hcc.CalcSpec.LoadtypesForPostprocessing = new List<string> {"Electricity"};
            int year = 2017;
            if (hcc.CalcSpec?.StartDate?.Year != null) {
#pragma warning disable S1944 // Inappropriate casts should not be made
                year = (int)hcc.CalcSpec.StartDate?.Year;
#pragma warning restore S1944 // Inappropriate casts should not be made
            }

            hcc.CalcSpec.EndDate = new DateTime(year, 1, 30);
            hcc.CalcSpec.CalcOptions = new List<CalcOption>();
            hcc.CalcSpec.CalcOptions.Add(CalcOption.SumProfileExternalIndividualHouseholdsAsJson);
            hcc.CalcSpec.ExternalTimeResolution = "00:15:00";
            var job = JsonConvert.SerializeObject(hcc, Formatting.Indented);
            string dstFn = Path.Combine(settings.ServerSettings.JsonDirectory[0], "myfile.json");
            File.WriteAllText(dstFn, job);

            Directory.CreateDirectory(tmpDir);
            Thread.Sleep(250);
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CC0022 // Should dispose object
#pragma warning disable IDE0067 // Dispose objects before losing scope
            DistLogger dl = new DistLogger(Path.Combine(tmpDir, "mylog.sqlite"), "log", _testOutputHelper);
#pragma warning restore IDE0067 // Dispose objects before losing scope
#pragma warning restore CC0022 // Should dispose object
#pragma warning restore CA2000 // Dispose objects before losing scope
            builder = new ContainerBuilder();
            builder.Register(x => dl).As<DistLogger>().SingleInstance();
            builder.Register(x => settings).As<Settings>().SingleInstance();
            builder.RegisterType<ServerThread>().SingleInstance();
            builder.RegisterType<ClientThread>();
            return settings;
        }

        [NotNull]
        private static Settings MakeTestSettings([NotNull] string tmpDir)
        {
            var clientSettings = new ClientSettings(Path.Combine(tmpDir, "c1"), 1);
            var serverSettings = new ServerSettings(@"V:\Dropbox\LPGReleases\releases8.9.0",
                new List<string> {Path.Combine(tmpDir, "JsonFiles")},
                Path.Combine(tmpDir, "FinishedCalcs"));
            Settings settings = new Settings(clientSettings, serverSettings, "tcp://127.0.0.1:5557");
            return settings;
        }

        [Fact]
        public void RunReportDiskFull()
        {
            const string tmpDir = @"c:\work\DistSimTests";
            var settings = PrepareTestEnviroment(tmpDir, out var builder);
            using (var container = builder.Build()) {
                StartServerThread(container);
                var clientThread = container.Resolve<ClientThread>(new TypedParameter(typeof(ThreadId), new ThreadId(Environment.MachineName, 1)));
                using (var rq = new RequestSocket()) {
                    rq.Connect(settings.ServerIP);
                    clientThread.ReportDiskFullAndWait(rq);
                    settings.ContinueRunning = false;
                    Thread.Sleep(5000);
                }
            }
        }

        [Fact]
        public void RunRequestForNewLpgFiles()
        {
            const string tmpDir = @"c:\work\DistSimTests";
            var settings = PrepareTestEnviroment(tmpDir, out var builder);
            using (var container = builder.Build()) {
                StartServerThread(container);
                var clientThread = container.Resolve<ClientThread>(new TypedParameter(typeof(ThreadId), new ThreadId(Environment.MachineName, 1)));
                using (var rq = new RequestSocket()) {
                    rq.Connect(settings.ServerIP);
                    clientThread.RequestAndSaveNewLPGFiles(rq);
                    settings.ContinueRunning = false;
                }
            }
        }

        [Fact]
        public void RunRequestNewJob()
        {
            const string tmpDir = @"c:\work\DistSimTests";
            var settings = PrepareTestEnviroment(tmpDir, out var builder);
            using (var container = builder.Build()) {
                var logger = container.Resolve<DistLogger>();
                var server = StartServerThread(container);

                var clientThread = container.Resolve<ClientThread>(new TypedParameter(typeof(ThreadId), new ThreadId(Environment.MachineName, 1)));
                using (var rq = new RequestSocket()) {
                    rq.Connect(settings.ServerIP);
                    var job = clientThread.RequestNewJobFromServer(rq);
                    if (job == null) {
                        throw new Exception("Job was null");
                    }

                    if (server.ThreadException != null) {
                        throw server.ThreadException;
                    }

                    clientThread.RequestAndSaveNewLPGFiles(rq);
                    CalcExecutor exe = new CalcExecutor(clientThread.ThreadId, container.Resolve<DistLogger>(), settings);
                    CalcDirectoryPreparer cdp = new CalcDirectoryPreparer(settings, logger, clientThread.ThreadId);
                    cdp.Run();
                    clientThread.ExecuteCalcJob(job, exe, rq);
                    Thread.Sleep(5000);
                    settings.ContinueRunning = false;
                }
            }
        }

        [Fact]
        public void RunServerJobRefreshTest()
        {
            const string tmpDir = @"c:\work\DistSimTests";
            var settings = PrepareTestEnviroment(tmpDir, out var builder);
            using (var container = builder.Build()) {
                var server = container.Resolve<ServerThread>(new TypedParameter(typeof(ThreadId), new ThreadId("Server", 1)));
                server.RefreshOpenTasks();
                settings.ContinueRunning = false;
            }
        }
    }
}