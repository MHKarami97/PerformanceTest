using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.MSSqlServer;

namespace ConsoleTest
{
    class Program
    {
        private const int MaxWcfCall = 100;
        private const string LogTableName = "SerilogWcfConsoleTest";

        private const string LogConnectionString =
            "Server=.;Database=LogDb;Persist Security Info=True;User ID=sa;Password=1qaz@WSX;MultipleActiveResultSets=True;";

        private static Logger _logger;
        private static Random _random;
        private static Stopwatch _stopwatch;

        private static bool _isFirstTime;
        private static ConcurrentQueue<string> _logQueue;

        static async Task Main(string[] args)
        {
            try
            {
                _isFirstTime = true;
                _logQueue = new ConcurrentQueue<string>();

                _random = new Random();
                _stopwatch = new Stopwatch();

                _stopwatch.Start();

                _logger = ConfigLogger();

                _stopwatch.Stop();

                Console.WriteLine($"config time: {_stopwatch.Elapsed}");

                _stopwatch.Reset();

                Console.WriteLine($"start time : {DateTime.Now:hh:mm:ss.ffffff}");
                _logger.Information("start time: {CallTime}", DateTime.Now);

                await CallWcf(CallType.SingleInstanceConcurrent);

                Console.WriteLine($"end time : {DateTime.Now:hh:mm:ss.ffffff}");
                _logger.Information("end time: {CallTime}", DateTime.Now);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.Error(e.Message);
            }
            finally
            {
                Log.CloseAndFlush();
                _logger.Dispose();
            }
        }

        private static Logger ConfigLogger()
        {
            var columnOpts = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn
                    {
                        ColumnName = "ThreadId", PropertyName = "ThreadId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "ManagedThreadId", PropertyName = "ManagedThreadId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "CallId", PropertyName = "CallId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "CallTime", PropertyName = "CallTime", DataType = SqlDbType.DateTime2
                    }
                }
            };

            columnOpts.Store.Remove(StandardColumn.Properties);
            columnOpts.Store.Remove(StandardColumn.MessageTemplate);
            columnOpts.Store.Remove(StandardColumn.Level);
            columnOpts.Store.Remove(StandardColumn.Exception);
            columnOpts.Store.Remove(StandardColumn.Message);

            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo
                .MSSqlServer(
                    connectionString: LogConnectionString,
                    columnOptions: columnOpts,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = LogTableName,
                        AutoCreateSqlTable = true,
                        BatchPostingLimit = 5,
                        BatchPeriod = TimeSpan.FromMilliseconds(100)
                    }
                )
                // .WriteTo
                // .Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
                // {
                //     AutoRegisterTemplate = true,
                //     AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
                // })
                .CreateLogger();
        }

        private static async Task CallWcf(CallType callType)
        {
            switch (callType)
            {
                case CallType.SingleInstanceConcurrent:
                    await CallWcfSingleInstanceConcurrent();
                    break;

                case CallType.SingleInstanceConcurrentAsParallel:
                    await CallWcfSingleInstanceConcurrentAsParallel();
                    break;

                case CallType.SingleInstance:
                    CallWcfSingleInstance();
                    break;

                case CallType.MultiInstanceConcurrent:
                    await CallWcfMultiInstanceConcurrent();
                    break;

                case CallType.MultiInstance:
                    CallWcfMultiInstance();
                    break;

                case CallType.MultiThread:
                    CallMultiThread();
                    break;

                case CallType.TaskFactory:
                    CallTaskFactory();
                    break;

                case CallType.TaskWithConfigureAwaitFalse:
                    await CallTaskWithConfigureAwaitFalse();
                    break;

                case CallType.TaskWithConfigureAwaitTrue:
                    await CallTaskWithConfigureAwaitTrue();
                    break;

                case CallType.ParallelLoop:
                    CallParallelLoop();
                    break;

                case CallType.MultiThreadWithConsoleLog:
                    await CallMultiThreadWithConsoleLog();
                    break;
            }
        }

        private static LoggableData GetLoggableData()
        {
            if (IsFirstTime())
            {
                _stopwatch.Start();
            }

            var time = DateTime.Now;
            var callId = _random.Next(0, int.MaxValue);
            var managedThreadId = Thread.CurrentThread.ManagedThreadId;
            var threadId = Process.GetCurrentProcess().Threads[0].Id;

            if (IsFirstTime())
            {
                ChangeIsFirstTime();

                _stopwatch.Stop();
                Console.WriteLine($"create log data time: {_stopwatch.Elapsed}");
                _stopwatch.Reset();
            }

            return new LoggableData
            {
                Time = time,
                CallId = callId,
                ManagementThreadId = managedThreadId,
                ThreadId = threadId
            };
        }

        private static bool IsFirstTime()
        {
            if (_isFirstTime)
            {
                return true;
            }

            return false;
        }

        private static void ChangeIsFirstTime()
        {
            if (_isFirstTime)
            {
                _isFirstTime = false;
            }
        }

        #region Services

        private static void CallWcfSingleInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                var loggableData = GetLoggableData();

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                Caller.Instance().Call(loggableData.CallId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId, DateTime.Now);
            }
        }

        private static async Task CallWcfSingleInstanceConcurrentAsParallel()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.Instance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                }));
            }

            await Task.WhenAll(tasks.AsParallel());
        }

        private static async Task CallWcfSingleInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.Instance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                }));
            }

            await Task.WhenAll(tasks);
        }

        private static void CallWcfMultiInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                var loggableData = GetLoggableData();

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);


                Caller.NewInstance().Call(loggableData.CallId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId, DateTime.Now);
            }
        }

        private static async Task CallWcfMultiInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.NewInstance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                }));
            }

            await Task.WhenAll(tasks.AsParallel());
        }

        private static void CallMultiThread()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.Instance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                });
            }
        }

        private static void CallTaskFactory()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.Instance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                }));
            }

            var taskItems = tasks.ToArray();

            Task.Factory.ContinueWhenAll(taskItems, completedTasks => { Console.WriteLine("finish"); });
        }

        private static async Task CallTaskWithConfigureAwaitFalse()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                await Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.Instance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                }).ConfigureAwait(false);
            }
        }

        private static async Task CallTaskWithConfigureAwaitTrue()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                await Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                    Caller.Instance().Call(loggableData.CallId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                        DateTime.Now);
                }).ConfigureAwait(true);
            }
        }

        private static void CallParallelLoop()
        {
            Parallel.For(0, MaxWcfCall, i =>
            {
                var loggableData = GetLoggableData();

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                Caller.Instance().Call(loggableData.CallId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId, DateTime.Now);
            });
        }

        private static async Task CallMultiThreadWithConsoleLog()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var loggableData = GetLoggableData();

                    _logQueue.Enqueue(
                        $"entry ,thread number: {loggableData.ThreadId}, managed thread id: {loggableData.ManagementThreadId}, call id: {loggableData.CallId}, call time: {loggableData.Time:hh:mm:ss.ffffff}");

                    Caller.Instance().CallWithOutServiceLog(loggableData.CallId);

                    _logQueue.Enqueue(
                        $"exit, callId: {loggableData.CallId}, call time: {DateTime.Now:hh:mm:ss.ffffff}");
                }));
            }

            await Task.WhenAll(tasks);

            foreach (var item in _logQueue)
            {
                Console.WriteLine(item);
            }
        }

        #endregion
    }
}