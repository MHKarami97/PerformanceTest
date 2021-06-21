using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkTest.ServiceReference1;

namespace BenchmarkTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Test>();
        }

        [EtwProfiler]
        [MemoryDiagnoser]
        [TailCallDiagnoser]
        [ThreadingDiagnoser]
        [KeepBenchmarkFiles]
        [NativeMemoryProfiler]
        [MinColumn, MaxColumn]
        [ConcurrencyVisualizerProfiler]
        [SimpleJob(RuntimeMoniker.Net50, baseline: true)]
        [SimpleJob(RuntimeMoniker.Net461, baseline: true)]
        [MarkdownExporter, HtmlExporter, CsvExporter, RPlotExporter]
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Test
        {
            private static HttpClient _httpClient;
            private static Service1Client _wcfClient;

            [GlobalSetup]
            public void Setup()
            {
                _httpClient = new HttpClient();
                _wcfClient = new Service1Client();
            }

            [Benchmark(Baseline = true)]
            public async Task Api()
            {
                try
                {
                    var response = await _httpClient.GetAsync("http://localhost:14920/api/Values/Get");
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            [Benchmark]
            public async Task Wcf()
            {
                try
                {
                    var data = await _wcfClient.GetDataAsync();
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}