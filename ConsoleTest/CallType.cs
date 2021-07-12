namespace ConsoleTest
{
    public enum CallType
    {
        SingleInstance,
        SingleInstanceConcurrent,
        SingleInstanceConcurrentAsParallel,
        MultiInstance,
        MultiInstanceConcurrent,
        MultiThread,
        TaskFactory,
        TaskWithConfigureAwaitFalse,
        TaskWithConfigureAwaitTrue,
        ParallelLoop,
        MultiThreadWithConsoleLog
    }
}