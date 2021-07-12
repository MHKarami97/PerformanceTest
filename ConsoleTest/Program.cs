using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
        private const int MaxWcfCall = 500;
        private const string LogTableName = "SerilogWcfConsoleTest";

        private const string LogConnectionString =
            "Server=.;Database=LogDb;Persist Security Info=True;User ID=sa;Password=1qaz@WSX;MultipleActiveResultSets=True;";

        private static Logger _logger;
        private static Random _random;

        static async Task Main(string[] args)
        {
            try
            {
                _logger = ConfigLogger();

                Console.WriteLine($"start time : {DateTime.Now:hh:mm:ss.ffffff}");

                await CallWcf(true, true);

                Console.WriteLine($"end time : {DateTime.Now:hh:mm:ss.ffffff}");
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

        private static async Task CallWcf(bool withSingleInstance, bool isConcurrent)
        {
            switch (withSingleInstance)
            {
                case true when isConcurrent:
                    await CallWcfSingleInstanceConcurrent();
                    break;

                case true when !isConcurrent:
                    CallWcfSingleInstance();
                    break;

                case false when isConcurrent:
                    await CallWcfMultiInstanceConcurrent();
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
                var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    threadId, managedThreadId, callId, DateTime.Now);

                WcfCall.Instance().CallWcf(callId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", callId, DateTime.Now);
            }
        }

        private static async Task CallWcfSingleInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var callId = _random.Next(0, int.MaxValue);
                    var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                    var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        threadId, managedThreadId, callId, DateTime.Now);

                    WcfCall.Instance().CallWcf(callId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", callId, DateTime.Now);
                }));
            }
            
            await Task.WhenAll(tasks.AsParallel());
        }

        private static void CallWcfMultiInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                var callId = _random.Next(0, int.MaxValue);
                var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    threadId, managedThreadId, callId, DateTime.Now);

                WcfCall.NewInstance().CallWcf(callId);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", callId, DateTime.Now);
            }
        }

        private static async Task CallWcfMultiInstanceConcurrent()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < MaxWcfCall; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var callId = _random.Next(0, int.MaxValue);
                    var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;
                    var managedThreadId = Thread.CurrentThread.ManagedThreadId;

                    _logger.Information(
                        "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                        threadId, managedThreadId, callId, DateTime.Now);

                    WcfCall.NewInstance().CallWcf(callId);

                    _logger.Information("exit, callId: {CallId}, call time: {CallTime}", callId, DateTime.Now);
                }));
            }

            await Task.WhenAll(tasks.AsParallel());
        }
    }
}