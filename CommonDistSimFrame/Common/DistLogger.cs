using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommonDistSimFrame.Client;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Xunit.Abstractions;

namespace CommonDistSimFrame.Common {
    public class DistLogger : IDisposable {
        [NotNull] private readonly Logger _logger;
        [NotNull] private readonly object _myLock = new object();
        [CanBeNull] private readonly ITestOutputHelper _testOutputHelper;

        public DistLogger([NotNull] string loggingFn, [NotNull] string table, [CanBeNull] ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            var lc = new LoggerConfiguration();
            if (_testOutputHelper != null) {
                _testOutputHelper.WriteLine("Logging to " + loggingFn);
            }

            lc = lc.WriteTo.SQLite(loggingFn, table);
            _logger = lc.CreateLogger();
        }

        [UsedImplicitly]
        [NotNull]
        [ItemNotNull]
        public ObservableCollection<LogMessage> LogCol { get; } = new ObservableCollection<LogMessage>();
        [UsedImplicitly]
        [NotNull]
        [ItemNotNull]
        public ObservableCollection<LogMessage> ErrorCol { get; } = new ObservableCollection<LogMessage>();

        [UsedImplicitly]
        public static Severity Threshold { get; set; } = Severity.Debug;

#pragma warning disable CC0029 // Disposables Should Call Suppress Finalize
        public void Dispose()
#pragma warning restore CC0029 // Disposables Should Call Suppress Finalize
        {
            _logger.Dispose();
        }

        public void Error([NotNull] string message, [NotNull] ThreadId threadId)
        {
            if (_testOutputHelper != null) {
                _testOutputHelper.WriteLine(message);
            }

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            _logger.Error(message, threadId);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            Report(message, Severity.Error);
        }

        public void Exception([NotNull] Exception ex, [NotNull] string message, [CanBeNull] [ItemCanBeNull] params object[] myparams)
        {
            if (_testOutputHelper != null) {
                _testOutputHelper.WriteLine(message + " " + ex.Message);
            }

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            _logger.Error(ex, message, myparams);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            Report(message + ": " + ex.Message, Severity.Exception);
        }

        public void Info([NotNull] string message, [NotNull] ThreadId thread, [CanBeNull] [ItemCanBeNull] params object[] myparams)
        {
            if (_testOutputHelper != null) {
                _testOutputHelper.WriteLine(thread.Name + ":" + message);
            }

            List<object> obs = new List<object>();
            obs.Add(thread);
            if (myparams != null) {
                foreach (var par in myparams) {
                    obs.Add(par);
                }
            }

#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            _logger.Information(message, obs.ToArray());
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            Report(message, Severity.Information);
        }

        [UsedImplicitly]
        public void Report([NotNull] string message, Severity severity)
        {
            if (severity > Threshold) {
                return;
            }

            Console.WriteLine(message);
            lock (_myLock) {
                void Action()
                {
                    while (LogCol.Count > 100) {
                        LogCol.RemoveAt(100);
                    }
                    while (ErrorCol.Count > 100)
                    {
                        LogCol.RemoveAt(100);
                    }

                    var lm = new LogMessage(message, severity);
                    LogCol.Insert(0, lm);

                    if (severity > Severity.Information) {
                        ErrorCol.Insert(0, lm);
                    }
                }

                SaveExecuteHelper.Get().SaveExecuteWithWait(Action);
            }
        }
    }
}