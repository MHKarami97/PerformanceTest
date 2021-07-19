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
        private const int CallCount = 100;
        private const string LogTableName = "SerilogConsoleTest";

        private const string LogConnectionString =
            "Server=.;Database=LogDb;Persist Security Info=True;User ID=sa;Password=1qaz@WSX;MultipleActiveResultSets=True;";

        private static Logger _logger;
        private static Random _random;
        private static Stopwatch _stopwatch;

        private static bool _isFirstTime;
        private static ConcurrentQueue<string> _logQueue;
        private const ServiceType Type = ServiceType.ApiHttpClient;
        private const CallType Call = CallType.SingleInstanceConcurrent;

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

                await CallWcf(Call);

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

        #region Config

        private static Logger ConfigLogger()
        {
            var columnOpts = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn
                    {
                        ColumnName = "CallTime", PropertyName = "CallTime", DataType = SqlDbType.DateTime2
                    },
                    new SqlColumn
                    {
                        ColumnName = "CallId", PropertyName = "CallId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "ThreadId", PropertyName = "ThreadId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "CurrentThreadId", PropertyName = "CurrentThreadId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "ManagedThreadId", PropertyName = "ManagedThreadId", DataType = SqlDbType.Int
                    },
                    new SqlColumn
                    {
                        ColumnName = "IsBackground", PropertyName = "IsBackground", DataType = SqlDbType.Bit
                    },
                    new SqlColumn
                    {
                        ColumnName = "IsThreadPoolThread", PropertyName = "IsThreadPoolThread", DataType = SqlDbType.Bit
                    },
                    new SqlColumn
                    {
                        ColumnName = "IsAlive", PropertyName = "IsAlive", DataType = SqlDbType.Bit
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
                    await CallWcfSingleInstance();
                    break;

                case CallType.MultiInstanceConcurrent:
                    await CallWcfMultiInstanceConcurrent();
                    break;

                case CallType.MultiInstance:
                    await CallWcfMultiInstance();
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

        #endregion

        #region Log

        private static LoggableData GetLoggableData()
        {
            if (IsFirstTime())
            {
                _stopwatch.Start();
            }

            var time = DateTime.Now;
            var callId = _random.Next(0, int.MaxValue);
            var managedThreadId = Thread.CurrentThread.ManagedThreadId;
            var isAlive = Thread.CurrentThread.IsAlive;
            var isBackground = Thread.CurrentThread.IsBackground;
            var isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
            var threadId = Process.GetCurrentProcess().Threads[0].Id;
            var currentThreadId = AppDomain.GetCurrentThreadId();

            if (IsFirstTime())
            {
                ChangeIsFirstTime();

                _stopwatch.Stop();
                Console.WriteLine($"create log time: {_stopwatch.Elapsed}");
                _stopwatch.Reset();
            }

            return new LoggableData
            {
                Time = time,
                CallId = callId,
                ManagementThreadId = managedThreadId,
                CurrentThreadId = currentThreadId,
                ThreadId = threadId,
                IsAlive = isAlive,
                IsBackground = isBackground,
                IsThreadPoolThread = isThreadPoolThread
            };
        }

        private static void LogFirst(LoggableData loggableData)
        {
            _logger.Information(
                "entry," +
                " thread number: {ThreadId}," +
                " managed thread id: {ManagedThreadId}," +
                " call id: {CallId}," +
                " call time: {CallTime}," +
                " currentThreadId: {CurrentThreadId}" +
                " isAlive: {IsAlive}," +
                " isBackground: {IsBackground}," +
                " isThreadPoolThread: {IsThreadPoolThread}",
                loggableData.ThreadId,
                loggableData.ManagementThreadId,
                loggableData.CallId,
                loggableData.Time,
                loggableData.CurrentThreadId,
                loggableData.IsAlive,
                loggableData.IsBackground,
                loggableData.IsThreadPoolThread);
        }

        private static void LogSecond(LoggableData loggableData)
        {
            _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId,
                DateTime.Now);
        }

        #endregion

        #region Services

        private static async Task CallWcfSingleInstance()
        {
            for (var i = 0; i < CallCount; i++)
            {
                var loggableData = GetLoggableData();

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                await Caller.Instance(Type).Call(loggableData.CallId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId, DateTime.Now);
            }
        }

        private static async Task CallWcfSingleInstanceConcurrentAsParallel()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < CallCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.Instance(Type).Call(loggableData.CallId);

                    LogSecond(loggableData);
                }));
            }

            await Task.WhenAll(tasks.AsParallel());
        }

        private static async Task CallWcfSingleInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < CallCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.Instance(Type).Call(loggableData.CallId);

                    LogSecond(loggableData);
                }));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task CallWcfMultiInstance()
        {
            for (var i = 0; i < CallCount; i++)
            {
                var loggableData = GetLoggableData();

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);


                await Caller.NewInstance().Call(loggableData.CallId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId, DateTime.Now);
            }
        }

        private static async Task CallWcfMultiInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < CallCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.NewInstance().Call(loggableData.CallId);

                    LogSecond(loggableData);
                }));
            }

            await Task.WhenAll(tasks.AsParallel());
        }

        private static void CallMultiThread()
        {
            for (var i = 0; i < CallCount; i++)
            {
                Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.Instance(Type).Call(loggableData.CallId);

                    LogSecond(loggableData);
                });
            }
        }

        private static void CallTaskFactory()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < CallCount; i++)
            {
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.Instance(Type).Call(loggableData.CallId);

                    LogSecond(loggableData);
                }));
            }

            var taskItems = tasks.ToArray();

            Task.Factory.ContinueWhenAll(taskItems, completedTasks => { Console.WriteLine("finish"); });
        }

        private static async Task CallTaskWithConfigureAwaitFalse()
        {
            for (var i = 0; i < CallCount; i++)
            {
                await Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.Instance(Type).Call(loggableData.CallId);

                    LogSecond(loggableData);
                }).ConfigureAwait(false);
            }
        }

        private static async Task CallTaskWithConfigureAwaitTrue()
        {
            for (var i = 0; i < CallCount; i++)
            {
                await Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    LogFirst(loggableData);

                    await Caller.Instance(Type).Call(loggableData.CallId);

                    LogSecond(loggableData);
                }).ConfigureAwait(true);
            }
        }

        private static void CallParallelLoop()
        {
            Parallel.For(0, CallCount, async i =>
            {
                var loggableData = GetLoggableData();

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    loggableData.ThreadId, loggableData.ManagementThreadId, loggableData.CallId, loggableData.Time);

                await Caller.Instance(Type).Call(loggableData.CallId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", loggableData.CallId, DateTime.Now);
            });
        }

        private static async Task CallMultiThreadWithConsoleLog()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < CallCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var loggableData = GetLoggableData();

                    _logQueue.Enqueue(
                        $"entry ,thread number: {loggableData.ThreadId}, managed thread id: {loggableData.ManagementThreadId}, call id: {loggableData.CallId}, call time: {loggableData.Time:hh:mm:ss.ffffff}");

                    await Caller.Instance(Type).CallWithOutServiceLog(loggableData.CallId);

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