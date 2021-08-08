using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc.Client;

namespace GRPC
{
    class Program
    {
        private const string MaxConcurrentStreams = "grpc.max_concurrent_streams";
        private const int MaxConcurrentStreamsValue = 1000;

        static async Task Main(string[] args)
        {
            var channel = new Channel("localhost",
                10042,
                ChannelCredentials.Insecure,
                new List<ChannelOption>
                {
                    new ChannelOption(MaxConcurrentStreams, MaxConcurrentStreamsValue)
                });

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

                Console.WriteLine(response.Result);
            }
            finally
            {
                await channel.ShutdownAsync();
            }
        }
    }
}