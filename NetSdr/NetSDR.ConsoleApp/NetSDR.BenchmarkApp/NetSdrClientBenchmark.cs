using BenchmarkDotNet.Attributes;
using NetSDR.Library.Models;
using NetSDR.Library.Services;

namespace NetSDR.BenchmarkApp;

public class NetSdrClientBenchmark
{
    private readonly NetSdrServer _netSdrServer = new(new TcpClientWrapper(), new UdpClientWrapper());

    [Params(100_000_000L, 200_000_000L)]
    public long FrequencyHz { get; set; }

    [Params(true, false)]
    public bool Start { get; set; }


    [GlobalSetup]
    public void GlobalSetup()
    {
        _netSdrServer.Connect("127.0.0.1", 50000, 60000);
    }

    [Benchmark]
    public void BenchmarkToggleReceiver()
    {
        _netSdrServer.ToggleReceiverState(Start, new ReceiverConfig());
    }

    [Benchmark]
    public void BenchmarkSetFrequency()
    {
        _netSdrServer.SetFrequency(FrequencyHz, new FrequencyConfig());
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _netSdrServer.Disconnect();
    }
}