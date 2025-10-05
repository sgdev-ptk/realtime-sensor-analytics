using Xunit;
using Processing;
using System.Reflection;

namespace Tests;

public class ProcessorMathTests
{
    [Fact]
    public void Percentile_Computes_P95()
    {
        var proc = CreateProcessorForTests();
        var values = new double[] { 1, 2, 3, 4, 5, 100 };
        var method = typeof(Processor).GetMethod("Percentile", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var p95 = (double)method!.Invoke(proc, new object[] { values, 0.95 })!;
        Assert.True(p95 >= 5 && p95 <= 100);
    }

    [Fact]
    public void StdDev_Zero_For_Single_Value()
    {
        var proc = CreateProcessorForTests();
        var method = typeof(Processor).GetMethod("StdDev", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = (double)method!.Invoke(proc, new object[] { new double[] { 42 }, })!;
        Assert.Equal(0, result);
    }

    private static Processor CreateProcessorForTests()
    {
        // Use reflection to bypass DI ctor: create with default/nulls where possible
        var ctor = typeof(Processor).GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
        // We cannot easily instantiate background deps; use TestDoubles via dynamic
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Processor>();
        var channel = System.Threading.Channels.Channel.CreateUnbounded<Processing.Models.Reading>();
        var frameSink = new TestSink();
        var store = new TestStore();
        return (Processor)ctor.Invoke(new object[] { logger, channel, frameSink, store });
    }

    private sealed class TestSink : IFrameSink { public void Add(Processing.Models.Reading r) { } }
    private sealed class TestStore : IStorageSink
    {
        public Task StoreAggregateAsync(Processing.Models.Aggregate aggregate, CancellationToken ct = default) => Task.CompletedTask;
        public Task StoreAlertAsync(Processing.Models.Alert alert, CancellationToken ct = default) => Task.CompletedTask;
        public Task StoreReadingAsync(Processing.Models.Reading reading, CancellationToken ct = default) => Task.CompletedTask;
    }
}
