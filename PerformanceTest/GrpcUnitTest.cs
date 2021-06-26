using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using GRPC;
using Grpc.Core;
using ProtoBuf.Grpc.Client;

namespace PerformanceTest
{
    [TestClass]
    public class GrpcUnitTest
    {
        private static ICalculator _calculator;

        public GrpcUnitTest()
        {
            var channel = new Channel("localhost", 10042, ChannelCredentials.Insecure);
            _calculator = channel.CreateGrpcService<ICalculator>();
        }

        [TestMethod]
        public async Task WcfMethod()
        {
            var response = await _calculator.MultiplyAsync(new MultiplyRequest
            {
                X = 2,
                Y = 4
            });

            if (response.Result != 8)
            {
                throw new InvalidOperationException();
            }
        }
    }
}