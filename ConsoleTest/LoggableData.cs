using System;

namespace ConsoleTest
{
    public class LoggableData
    {
        public int CallId { get; set; }
        public int ThreadId { get; set; }
        public int ManagementThreadId { get; set; }
        public int CurrentThreadId { get; set; }
        public DateTime Time { get; set; }
        public bool IsAlive { get; set; }
        public bool IsBackground { get; set; }
        public bool IsThreadPoolThread { get; set; }
    }
}