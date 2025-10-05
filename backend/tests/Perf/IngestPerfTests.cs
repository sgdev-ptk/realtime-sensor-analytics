using Xunit;
using Xunit.Abstractions;

namespace Perf;

// Suppress documentation requirement for test methods
#pragma warning disable SA1600
/// <summary>
/// Perf observation over the live SignalR stream. Skips unless PERF_RUN=1.
/// </summary>
public class IngestPerfTests(ITestOutputHelper output)
{
    [Fact]
    /// <summary>
    /// Connects to the SignalR hub, joins a sensor group, and records p95 latency and observed rate for a short window.
    /// </summary>
    public async Task Observe_p95_latency_and_rate_via_stream_for_30s()
    {
        // Default to skip unless explicitly enabled
        var perfRun = Environment.GetEnvironmentVariable("PERF_RUN") == "1";
        if (!perfRun || Environment.GetEnvironmentVariable("PERF_SKIP") == "1")
        {
            output.WriteLine("Perf test skipped. Set PERF_RUN=1 to enable.");
            return;
        }

        // Config via env with sensible defaults for local dev
        var baseUrl = Environment.GetEnvironmentVariable("PERF_BASE_URL") ?? "https://localhost:5001";
        var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "dev";
        var sensor = Environment.GetEnvironmentVariable("PERF_SENSOR") ?? "sensor-1";
        var seconds = int.TryParse(Environment.GetEnvironmentVariable("PERF_DURATION_SEC"), out var s) ? s : 30;
        var p95BudgetMs = int.TryParse(Environment.GetEnvironmentVariable("PERF_P95_BUDGET_MS"), out var b) ? b : 250;
        var minObservedRate = int.TryParse(Environment.GetEnvironmentVariable("PERF_MIN_RATE"), out var r) ? r : 800;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds + 10));
        var results = await PerfUtil.RunAsync(baseUrl, apiKey, sensor, TimeSpan.FromSeconds(seconds), cts.Token);

        output.WriteLine($"Points={results.Points} rate={results.ObservedRatePerSec:F1}/s p95={results.P95LatencyMs:F1}ms duration={results.DurationSeconds:F1}s");

        // Soft assertions: only assert if budgets are positive; otherwise just log for manual inspection
        if (p95BudgetMs > 0)
        {
            Assert.True(results.P95LatencyMs <= p95BudgetMs, $"p95 {results.P95LatencyMs:F1}ms > budget {p95BudgetMs}ms");
        }

        if (minObservedRate > 0)
        {
            Assert.True(results.ObservedRatePerSec >= minObservedRate, $"rate {results.ObservedRatePerSec:F1}/s < min {minObservedRate}/s");
        }
    }
}
#pragma warning restore SA1600
