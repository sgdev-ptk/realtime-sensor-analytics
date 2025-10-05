using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace Perf;

public static class PerfUtil
{
    public sealed record ReadingDto(string SensorId, DateTimeOffset Ts, double Value, string Status);

    public sealed record PerfResults(
        int Points,
        double DurationSeconds,
        double ObservedRatePerSec,
        double P95LatencyMs,
        IReadOnlyList<double> LatenciesMs);

    public static async Task<PerfResults> RunAsync(
        string baseUrl,
        string apiKey,
        string sensorId,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var endpoint = baseUrl.TrimEnd('/') + "/api/stream";
        var latencies = new ConcurrentBag<double>();
        var totalPoints = 0;

        var connection = new HubConnectionBuilder()
            .WithUrl(endpoint, options =>
            {
                options.Headers.Add("x-api-key", apiKey);

                // Allow self-signed dev certs for local runs
                options.HttpMessageHandlerFactory = (msg) =>
                {
                    if (msg is HttpClientHandler h)
                    {
                        h.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }

                    return msg;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<List<ReadingDto>>("frame", (frame) =>
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var r in frame)
            {
                if (r.SensorId == sensorId)
                {
                    totalPoints++;
                    var ms = (now - r.Ts).TotalMilliseconds;
                    latencies.Add(ms);
                }
            }
        });

        await connection.StartAsync(cancellationToken);
        await connection.InvokeAsync("Join", sensorId, cancellationToken);

        var started = DateTimeOffset.UtcNow;
        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            try
            {
                await connection.InvokeAsync("Leave", sensorId, cancellationToken);
            }
            catch
            {
                // ignore
            }

            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }

        var elapsed = (DateTimeOffset.UtcNow - started).TotalSeconds;
        var latList = latencies.ToArray();
        Array.Sort(latList);
        var p95 = latList.Length == 0 ? 0 : Percentile(latList, 0.95);

        return new PerfResults(
            Points: totalPoints,
            DurationSeconds: elapsed,
            ObservedRatePerSec: elapsed > 0 ? totalPoints / elapsed : 0,
            P95LatencyMs: p95,
            LatenciesMs: latList);
    }

    private static double Percentile(double[] sorted, double p)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var idx = (int)Math.Ceiling(p * sorted.Length) - 1;
        idx = Math.Clamp(idx, 0, sorted.Length - 1);
        return sorted[idx];
    }
}
