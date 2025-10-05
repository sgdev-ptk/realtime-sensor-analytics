namespace Processing;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Processing.Models;
using Prometheus;

#pragma warning disable SA1600 // Elements should be documented
public sealed class Processor(ILogger<Processor> logger, Channel<Reading> channel, IFrameSink frameSink, IStorageSink store) : BackgroundService
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

    private static readonly ActivitySource Trace = new("Processing.Processor");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = channel.Reader;
        var batch = new List<Reading>(MaxBatchSize);

        // Per-sensor Welford state
        var state = new ConcurrentDictionary<string, Welford>();

        var sw = Stopwatch.StartNew();
        var oneMinute = TimeSpan.FromMinutes(1);
        var windowBuffers = new ConcurrentDictionary<string, Queue<Reading>>();
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
            using var activity = Trace.StartActivity("process_batch", ActivityKind.Internal);
            activity?.SetTag("batch.count", batch.Count);
            foreach (var r in batch)
            {
                var w = state.GetOrAdd(r.SensorId, _ => new Welford());
                w.Add(r.Value);
                frameSink.Add(r);
                ReadingsProcessed.Inc();

                // Persist raw reading with TTL
                _ = store.StoreReadingAsync(r, stoppingToken);

                // Maintain 1-minute buffer per-sensor
                var q = windowBuffers.GetOrAdd(r.SensorId, _ => new Queue<Reading>(512));
                q.Enqueue(r);

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
                        activity?.AddEvent(new ActivityEvent("anomaly", tags: new ActivityTagsCollection
                        {
                            { "sensor", r.SensorId },
                            { "z", z },
                            { "value", r.Value },
                        }));
                    }
                }
            }

            // Compute per-sensor and global aggregates over the last minute
            var cutoff = DateTimeOffset.UtcNow - oneMinute;
            var global = new List<double>();
            var perSensorAggs = 0;
            foreach (var kvp in windowBuffers)
            {
                var sensorId = kvp.Key;
                var q = kvp.Value;
                while (q.Count > 0 && q.Peek().Ts < cutoff)
                {
                    q.Dequeue();
                }

                if (q.Count == 0)
                {
                    continue;
                }

                var values = q.Select(x => x.Value).ToArray();
                global.AddRange(values);
                var agg = new Aggregate
                {
                    SensorId = sensorId,
                    Window = Window.W1m,
                    Count = values.LongLength,
                    Min = values.Min(),
                    Max = values.Max(),
                    Mean = values.Average(),
                    Stdev = this.StdDev(values),
                    P95 = this.Percentile(values, 0.95),
                };
                _ = store.StoreAggregateAsync(agg, stoppingToken);
                perSensorAggs++;
            }

            if (global.Count > 0)
            {
                var gv = global.ToArray();
                var gagg = new Aggregate
                {
                    SensorId = null,
                    Window = Window.W1m,
                    Count = gv.LongLength,
                    Min = gv.Min(),
                    Max = gv.Max(),
                    Mean = gv.Average(),
                    Stdev = this.StdDev(gv),
                    P95 = this.Percentile(gv, 0.95),
                };
                _ = store.StoreAggregateAsync(gagg, stoppingToken);
            }

            BatchesProcessed.Inc();
            BatchSizes.Observe(batch.Count);
            logger.LogInformation("Processed batch size={BatchSize} perSensorAggs={Aggs}", batch.Count, perSensorAggs);
            activity?.SetTag("aggregates.per_sensor", perSensorAggs);
        }
    }

    private double StdDev(double[] values)
    {
        if (values.Length <= 1)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values.Select(v => (v - mean) * (v - mean)).Sum() / (values.Length - 1);
        return Math.Sqrt(variance);
    }

    private double Percentile(double[] values, double p)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        Array.Sort(values);
        var idx = (int)Math.Ceiling(p * values.Length) - 1;
        idx = Math.Clamp(idx, 0, values.Length - 1);
        return values[idx];
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
