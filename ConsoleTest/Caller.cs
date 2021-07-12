using System.Net.Http;
using ConsoleTest.ServiceReference;

namespace ConsoleTest
{
    public class Caller
    {
        private static Caller _instance;
        private static HttpClient _httpClient;
        private static Service1Client _service;
        private static readonly object Lock = new object();
        private readonly ServiceType _serviceType;

        private Caller(ServiceType serviceType)
        {
            _serviceType = serviceType;

            switch (_serviceType)
            {
                case ServiceType.Wcf:
                    _service = new Service1Client();
                    break;

                case ServiceType.Api:
                    _httpClient = new HttpClient();
                    break;
            }
        }

        public static Caller Instance(ServiceType serviceType = ServiceType.Wcf)
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Caller(serviceType);
                    }
                }
            }

            return _instance;
        }

        public static Caller NewInstance(ServiceType serviceType = ServiceType.Wcf)
        {
            return new Caller(serviceType);
        }

        public void Call(int callId)
        {
            switch (_serviceType)
            {
                case ServiceType.Wcf:
                    var result = _service.GetData(callId);
                    break;

                case ServiceType.Api:
                    break;
            }
        }

        public void CallWithOutServiceLog(int callId)
        {
            switch (_serviceType)
            {
                case ServiceType.Wcf:
                    var result = _service.GetDataWithoutLog(callId);
                    break;

                case ServiceType.Api:
                    break;
            }
        }
    }
}