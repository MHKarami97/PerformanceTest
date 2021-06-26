using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc.Client;

namespace GRPC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = new Channel("localhost", 10042, ChannelCredentials.Insecure);
            try
            {
                var calculator = channel.CreateGrpcService<ICalculator>();
                
                var response = await calculator.MultiplyAsync(new MultiplyRequest
                {
                    X = 2,
                    Y = 4
                });
                
                if (response.Result != 8)
                {
                    throw new InvalidOperationException();
                }
            }
            finally
            {
                await channel.ShutdownAsync();
            }
        }
    }
    
    [DataContract]
    public class MultiplyRequest
    {
        [DataMember(Order = 1)]
        public int X { get; set; }

        [DataMember(Order = 2)]
        public int Y { get; set; }
    }

    [DataContract]
    public class MultiplyResult
    {
        [DataMember(Order = 1)]
        public int Result { get; set; }
    }
    
    [ServiceContract(Name = "Hyper.Calculator")]
    public interface ICalculator
    {
        ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request);
    }
}