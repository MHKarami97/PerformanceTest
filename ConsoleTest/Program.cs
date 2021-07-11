using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.MSSqlServer;

namespace ConsoleTest
{
    class Program
    {
        private const int MaxWcfCall = 500;
        private const string LogTableName = "SerilogWcfConsoleTest";

        private const string LogConnectionString =
            "Server=.;Database=LogDb;Persist Security Info=True;User ID=sa;Password=1qaz@WSX;MultipleActiveResultSets=True;";

        private static Logger _logger;
        private static Random _random;

        static void Main(string[] args)
        {
            try
            {
                _logger = ConfigLogger();

                Console.WriteLine($"start time : {DateTime.Now.ToString("hh:mm:ss.ffffff")}");

                CallWcf(true, true);

                Console.WriteLine($"end time : {DateTime.Now.ToString("hh:mm:ss.ffffff")}");
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
            _random = new Random();

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
                        ColumnName = "CallId", PropertyName = "CallId", DataType = SqlDbType.NVarChar
                    }
                }
            };

            columnOpts.Store.Remove(StandardColumn.Properties);
            columnOpts.Store.Remove(StandardColumn.MessageTemplate);
            columnOpts.Store.Remove(StandardColumn.Level);
            columnOpts.Store.Remove(StandardColumn.Exception);

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
                ).CreateLogger();
        }

        private static void CallWcf(bool withSingleInstance, bool isConcurrent)
        {
            switch (withSingleInstance)
            {
                case true when isConcurrent:
                    CallWcfSingleInstanceConcurrent();
                    break;

                case true when !isConcurrent:
                    CallWcfSingleInstance();
                    break;

                case false when isConcurrent:
                    CallWcfMultiInstanceConcurrent();
                    break;

                case false when !isConcurrent:
                    CallWcfMultiInstance();
                    break;
            }
        }

        private static void CallWcfSingleInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                var callId = _random.Next(0, int.MaxValue);
                var caller = callId.ToString("N");
                var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}",
                    threadId, managedThreadId, caller);

                WcfCall.Instance().CallWcf(callId);

                _logger.Information("exit, callId: {CallId}", caller);
            }
        }

        private static void CallWcfSingleInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var callId = _random.Next(0, int.MaxValue);
                    var caller = callId.ToString("N");
                    var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                    var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}",
                        threadId, managedThreadId, caller);

                    WcfCall.Instance().CallWcf(callId);

                    _logger.Information("exit, callId: {CallId}", caller);
                }));
            }

            var taskItems = tasks.ToArray();

            Task.WaitAll(taskItems);
        }

        private static void CallWcfMultiInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                var callId = _random.Next(0, int.MaxValue);
                var caller = callId.ToString("N");
                var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}",
                    threadId, managedThreadId, caller);

                WcfCall.NewInstance().CallWcf(callId);

                _logger.Information("exit, callId: {CallId}", caller);
            }
        }

        private static void CallWcfMultiInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var callId = _random.Next(0, int.MaxValue);
                    var caller = callId.ToString("N");
                    var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                    var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}",
                        threadId, managedThreadId, caller);

                    WcfCall.NewInstance().CallWcf(callId);

                    _logger.Information("exit, callId: {CallId}", caller);
                }));
            }

            var taskItems = tasks.ToArray();

            Task.WaitAll(taskItems);
        }
    }
}