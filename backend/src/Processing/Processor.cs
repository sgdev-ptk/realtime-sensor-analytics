using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Processing.Models;
using Prometheus;

namespace Processing;

#pragma warning disable SA1600 // Elements should be documented
public sealed class Processor(ILogger<Processor> logger, Channel<Reading> channel, IFrameSink frameSink) : BackgroundService
{
    private const int MaxBatchSize = 500;
    private static readonly TimeSpan MaxBatchWindow = TimeSpan.FromMilliseconds(50);
    private static readonly Counter ReadingsProcessed = Metrics.CreateCounter(
        "processor_readings_total", "Number of readings processed");

    private static readonly Counter BatchesProcessed = Metrics.CreateCounter(
        "processor_batches_total", "Number of batches processed");

    private static readonly Histogram BatchSizes = Metrics.CreateHistogram(
        "processor_batch_size", "Batch sizes of readings", new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(start: 10, width: 50, count: 12),
        });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = channel.Reader;
        var batch = new List<Reading>(MaxBatchSize);

        // Per-sensor Welford state
        var state = new ConcurrentDictionary<string, Welford>();

        var sw = Stopwatch.StartNew();
        while (!stoppingToken.IsCancellationRequested)
        {
            batch.Clear();

            // Attempt to read at least one item or wait briefly
            if (await reader.WaitToReadAsync(stoppingToken))
            {
                var started = sw.Elapsed;
                while (batch.Count < MaxBatchSize && (sw.Elapsed - started) < MaxBatchWindow && reader.TryRead(out var item))
                {
                    batch.Add(item);
                }

                // If we only got one by TryRead fast path, ensure we read the first item if needed
                while (batch.Count < MaxBatchSize && (sw.Elapsed - started) < MaxBatchWindow && reader.TryRead(out var next))
                {
                    batch.Add(next);
                }
            }

            if (batch.Count == 0)
            {
                // Small delay to avoid busy loop when no data
                await Task.Delay(5, stoppingToken);
                continue;
            }

            // Process batch
            foreach (var r in batch)
            {
                var w = state.GetOrAdd(r.SensorId, _ => new Welford());
                w.Add(r.Value);
                frameSink.Add(r);
                ReadingsProcessed.Inc();

                // Simple z-score anomaly detection
                if (w.Count >= 30 && w.StdDev > 1e-9)
                {
                    var z = Math.Abs((r.Value - w.Mean) / w.StdDev);
                    if (z >= 3.0)
                    {
                        // TODO: publish alert in later tasks (T022/T033)
                        logger.LogDebug(
                            "Anomaly detected sensor={Sensor} z={Z:F2} value={Value:F2} mean={Mean:F2} std={Std:F2}",
                            r.SensorId,
                            z,
                            r.Value,
                            w.Mean,
                            w.StdDev);
                    }
                }
            }

            BatchesProcessed.Inc();
            BatchSizes.Observe(batch.Count);
        }
    }

    private sealed class Welford
    {
        private double mean;
        private double m2;

        public int Count { get; private set; }

        public double Mean => this.mean;

        public double Variance => this.Count > 1 ? this.m2 / (this.Count - 1) : 0.0;

        public double StdDev => Math.Sqrt(this.Variance);

        public void Add(double x)
        {
            this.Count++;
            var delta = x - this.mean;
            this.mean += delta / this.Count;
            var delta2 = x - this.mean;
            this.m2 += delta * delta2;
        }
    }
}
#pragma warning restore SA1600 // Elements should be documented
