using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerformanceTest.ServiceReference1;
using System;

namespace PerformanceTest
{
    [TestClass]
    public class WcfUnitTest
    {
        private static Service1Client _wcfClient;

        public WcfUnitTest()
        {
            _wcfClient = new Service1Client();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var data = _wcfClient.GetData();
        }
    }
}