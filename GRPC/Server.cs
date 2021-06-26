using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc.Server;

namespace GRPC
{
    public class ServerGrpc
    {
        static async Task Main2()
        {
            try
            {
                const int port = 10042;

                var server = new Server
                {
                    Ports =
                    {
                        new ServerPort("localhost", port, ServerCredentials.Insecure)
                    }
                };

                server.Services.AddCodeFirst(new MyCalculator());
            
                server.Start();

                Console.WriteLine("server listening on port " + port);
            
                Console.ReadKey();
                
                await server.ShutdownAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
                throw;
            }
        }
    }
    
    [ServiceContract(Name = "Hyper.Calculator")]
    public interface ICalculatorServer
    {
        ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request, ServerCallContext context = default);
    }

    class MyCalculator : ICalculatorServer
    {
        public ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request, ServerCallContext context = default)
        {
            Console.WriteLine($"Processing request from {context.Peer}");
            
            var result = request.X * request.Y;

            return new ValueTask<MultiplyResult>();
        }
    }
}