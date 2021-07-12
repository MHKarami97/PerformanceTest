using System;

namespace ConsoleTest
{
    public class LoggableData
    {
        public int CallId { get; set; }
        public int ThreadId { get; set; }
        public int ManagementThreadId { get; set; }
        public DateTime Time { get; set; }
    }
}