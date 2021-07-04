using Grpc.Core;
using ProtoBuf.Grpc.Server;
using System;
using System.Threading.Tasks;
using GRPC;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace GRPcService
{
    class Program
    {
        static async Task Main(string[] args)
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

                var generator = new SchemaGenerator
                {
                    ProtoSyntax = ProtoSyntax.Proto3
                };

                var schema = generator.GetSchema<ICalculator>(); // there is also a non-generic overload that takes Type

                using (var writer = new System.IO.StreamWriter("services.proto"))
                {
                    await writer.WriteAsync(schema);
                }

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

    public class MyCalculator : ICalculator
    {
        ValueTask<MultiplyResult> ICalculator.MultiplyAsync(MultiplyRequest request)
        {
            var result = new MultiplyResult {Result = request.X * request.Y};
            return new ValueTask<MultiplyResult>(result);
        }
    }
}