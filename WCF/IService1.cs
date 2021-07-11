using System.ServiceModel;

namespace WCF
{
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        string GetData(int callId);
    }
}