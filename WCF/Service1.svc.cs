using System;
using System.Collections.ObjectModel;
using System.Data;
using System.ServiceModel;
using System.Threading;
using Serilog;
using Serilog.Sinks.MSSqlServer;

namespace WCF
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class Service1 : IService1, IDisposable
    {
        private const int SleepTimeInMillisecond = 30;
        private const string LogTableName = "SerilogWcfService";

        private const string LogConnectionString =
            "Server=.;Database=LogDb;Persist Security Info=True;User ID=sa;Password=1qaz@WSX;MultipleActiveResultSets=True;";

        private static Serilog.Core.Logger _logger;

        public Service1()
        {
            ConfigLogger();
        }

        private static void ConfigLogger()
        {
            var columnOpts = new ColumnOptions();

            columnOpts.Store.Remove(StandardColumn.Properties);
            columnOpts.Store.Remove(StandardColumn.MessageTemplate);
            columnOpts.Store.Remove(StandardColumn.Level);
            columnOpts.Store.Remove(StandardColumn.Exception);
            columnOpts.Store.Remove(StandardColumn.Message);

            columnOpts.AdditionalColumns = new Collection<SqlColumn>
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
            };

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo
                .MSSqlServer(
                    connectionString: LogConnectionString,
                    columnOptions: columnOpts,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = LogTableName,
                        AutoCreateSqlTable = true,
                        BatchPostingLimit = 1,
                        BatchPeriod = TimeSpan.FromMilliseconds(1)
                    }
                ).CreateLogger();
        }

        public string GetData(int callId)
        {
            try
            {
                var managedThreadId = Thread.CurrentThread.ManagedThreadId;
                var threadId = System.Diagnostics.Process.GetCurrentProcess().Threads[0].Id;

                _logger.Information(
                    "entry ,thread number: {ThreadId}, managed thread id: {ManagedThreadId}, call id: {CallId}, call time: {CallTime}",
                    threadId, managedThreadId, callId, DateTime.Now);

                Thread.Sleep(SleepTimeInMillisecond);

                _logger.Information("exit, callId: {CallId}, call time: {CallTime}", callId, DateTime.Now);
            }
            catch (Exception e)
            {
                _logger.Error(e, "ex");
            }

            return "data";
        }

        public string GetDataWithoutLog(int callId)
        {
            Thread.Sleep(SleepTimeInMillisecond);

            return "data";
        }

        public void Dispose()
        {
            _logger.Dispose();
            Log.CloseAndFlush();
        }
    }
}