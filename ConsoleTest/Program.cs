namespace ConsoleTest
{
    class Program
    {
        private const int MaxWcfCall = 500;

        static void Main(string[] args)
        {
            CallWcf(true);
        }

        private static void CallWcf(bool withSingleInstance)
        {
            if (withSingleInstance)
            {
                CallWcfSingleInstance();
            }
            else
            {
                CallWcfMultiInstance();
            }
        }

        private static void CallWcfSingleInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                WcfCall.Instance().CallWcf();
            }
        }

        private static void CallWcfMultiInstance()
        {
            for (var i = 0; i < MaxWcfCall; i++)
            {
                WcfCall.NewInstance().CallWcf();
            }
        }
    }
}