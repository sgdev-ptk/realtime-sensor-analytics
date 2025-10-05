namespace Api;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Processing;
using Processing.Models;
using Prometheus;
using System.Diagnostics;

#pragma warning disable SA1600 // Elements should be documented
public sealed class FrameBroadcaster(ILogger<FrameBroadcaster> logger, IHubContext<StreamHub> hub)
    : BackgroundService, IFrameSink
{
    private static readonly Counter FramesSent = Metrics.CreateCounter(
        "frame_broadcaster_frames_sent_total", "Number of coalesced frames sent to clients");

    private static readonly Counter PointsSent = Metrics.CreateCounter(
        "frame_broadcaster_points_sent_total", "Number of individual readings included in frames");

    private static readonly Counter SendErrors = Metrics.CreateCounter(
        "frame_broadcaster_send_errors_total", "Number of errors while sending frames");

    private readonly ILogger<FrameBroadcaster> log = logger;
    private readonly IHubContext<StreamHub> hubContext = hub;
    private readonly ConcurrentDictionary<string, List<Reading>> buffers = new();
    private static readonly ActivitySource Trace = new("Api.FrameBroadcaster");

    public void Add(Reading reading)
    {
        var list = this.buffers.GetOrAdd(reading.SensorId, _ => new List<Reading>(256));
        lock (list)
        {
            list.Add(reading);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(50); // ~20 FPS
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            foreach (var kvp in this.buffers)
            {
                var sensorId = kvp.Key;
                var list = kvp.Value;
                List<Reading> snapshot;
                lock (list)
                {
                    if (list.Count == 0)
                    {
                        continue;
                    }
                    snapshot = new List<Reading>(list);
                    list.Clear();
                }

                try
                {
                    using var activity = Trace.StartActivity("send_frame", ActivityKind.Producer);
                    activity?.SetTag("sensor", sensorId);
                    activity?.SetTag("points", snapshot.Count);
                    await this.hubContext.Clients.Group(StreamHub.GroupFor(sensorId))
                        .SendAsync("frame", snapshot, cancellationToken: stoppingToken);
                    FramesSent.Inc();
                    PointsSent.Inc(snapshot.Count);
                    this.log.LogDebug("Sent frame sensor={Sensor} points={Count}", sensorId, snapshot.Count);
                }
                catch (Exception ex)
                {
                    SendErrors.Inc();
                    this.log.LogWarning(ex, "Failed to send frame for sensor {SensorId}", sensorId);
                }
            }
        }
    }
}
#pragma warning restore SA1600 // Elements should be documented
