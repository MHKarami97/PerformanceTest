using ConsoleTest.ServiceReference;

namespace ConsoleTest
{
    public class WcfCall
    {
        private static WcfCall _instance;
        private static Service1Client _service;
        private static readonly object Lock = new object();

        private WcfCall()
        {
            _service = new Service1Client();
        }

        public static WcfCall Instance()
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new WcfCall();
                    }
                }
            }

            return _instance;
        }

        public static WcfCall NewInstance()
        {
            return new WcfCall();
        }

        public void CallWcf(int callId)
        {
            var result = _service.GetData(callId);
        }
        
        public void CallWcfWithoutServiceLog(int callId)
        {
            var result = _service.GetDataWithoutLog(callId);
        }
    }
}