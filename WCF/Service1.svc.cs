using System.ServiceModel;
using System.Threading;

namespace WCF
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class Service1 : IService1
    {
        private const int SleepTimeInMillisecond = 30;
        
        public string GetData()
        {
            Thread.Sleep(SleepTimeInMillisecond);
            
            return "data";
        }
    }
}
