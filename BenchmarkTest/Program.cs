using System;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
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

        [DryJob]
        //***
        [EtwProfiler]
        [MemoryDiagnoser]
        [TailCallDiagnoser]
        //[ThreadingDiagnoser]
        [NativeMemoryProfiler]
        //[ConcurrencyVisualizerProfiler]
        [KeepBenchmarkFiles]
        //***
        [MinColumn, MaxColumn]
        //***
        [JsonExporterAttribute.Brief]
        [JsonExporterAttribute.Full]
        [JsonExporterAttribute.FullCompressed]
        [JsonExporterAttribute.BriefCompressed]
        //***
        [SimpleJob(targetCount: 100)]
        [SimpleJob(RunStrategy.Monitoring)]
        [SimpleJob(RuntimeMoniker.Net50)]
        [SimpleJob(RuntimeMoniker.Net461, baseline: true)]
        //***
        [MarkdownExporter, HtmlExporter, CsvExporter, RPlotExporter]
        //***
        [RankColumn(NumeralSystem.Stars)]
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