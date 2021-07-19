using System.Net.Http;
using System.Threading.Tasks;
using ConsoleTest.ServiceReference;
using Flurl.Http;
using RestSharp;

namespace ConsoleTest
{
    public class Caller
    {
        private static Caller _instance;
        private static HttpClient _httpClient;
        private static RestClient _restClient;
        private static Service1Client _service;
        private static readonly object Lock = new object();
        private readonly ServiceType _serviceType;

        private static string _apiUrl;

        private Caller(ServiceType serviceType)
        {
            _apiUrl = "http://localhost/api/Values/get";

            _serviceType = serviceType;

            switch (_serviceType)
            {
                case ServiceType.Wcf:
                    _service = new Service1Client();
                    break;

                case ServiceType.ApiHttpClient:
                    _httpClient = new HttpClient();
                    break;

                case ServiceType.ApiRestSharp:
                    _restClient = new RestClient(_apiUrl);
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

        public async Task Call(int callId)
        {
            switch (_serviceType)
            {
                case ServiceType.Wcf:
                    await _service.GetDataAsync(callId);
                    break;

                case ServiceType.ApiHttpClient:
                    await _httpClient.GetAsync(_apiUrl);
                    break;

                case ServiceType.ApiRestSharp:
                    await _restClient.GetAsync<string>(new RestRequest());
                    break;

                case ServiceType.ApiFlurl:
                    await _apiUrl.GetAsync();
                    break;
            }
        }

        public async Task CallWithOutServiceLog(int callId)
        {
            switch (_serviceType)
            {
                case ServiceType.Wcf:
                    await _service.GetDataWithoutLogAsync(callId);
                    break;

                case ServiceType.ApiHttpClient:
                    await _httpClient.GetAsync(_apiUrl);
                    break;

                case ServiceType.ApiRestSharp:
                    await _restClient.GetAsync<string>(new RestRequest());
                    break;

                case ServiceType.ApiFlurl:
                    await _apiUrl.GetAsync();
                    break;
            }
        }
    }
}