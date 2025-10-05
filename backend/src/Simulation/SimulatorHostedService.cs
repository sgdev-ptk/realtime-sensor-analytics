using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Processing.Models;

namespace Simulation;

public sealed class SimulatorHostedService(ILogger<SimulatorHostedService> logger, Channel<Reading> channel, IConfiguration config)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    var sensors = config.GetValue<int>("SIM__SENSORS", 10);
    var ratePerSec = config.GetValue<int>("SIM__RATE", 1000);
        var interval = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, ratePerSec));
        var rnd = new Random();

        logger.LogInformation("Simulator starting with {Sensors} sensors at ~{Rate}/s", sensors, ratePerSec);
        var writer = channel.Writer;

        var sensorIds = Enumerable.Range(1, sensors).Select(i => $"sensor-{i}").ToArray();

        while (!stoppingToken.IsCancellationRequested)
        {
            var ts = DateTimeOffset.UtcNow;
            foreach (var id in sensorIds)
            {
                var value = rnd.NextDouble() * 100.0; // 0-100 for demo
                var reading = new Reading { SensorId = id, Ts = ts, Value = value };
                await writer.WriteAsync(reading, stoppingToken);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        writer.Complete();
        logger.LogInformation("Simulator stopped");
    }
}
