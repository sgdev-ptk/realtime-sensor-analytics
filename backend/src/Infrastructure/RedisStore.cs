namespace Infrastructure;

using System.Text.Json;
using Processing.Models;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

public sealed class RedisStore(ILogger<RedisStore> logger) : IRedisStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private readonly ILogger<RedisStore> logger = logger;
    private readonly Lazy<ConnectionMultiplexer> conn = new(() =>
    {
        var cs = Environment.GetEnvironmentVariable("REDIS__CONNECTION") ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(cs);
    });

    private IDatabase Db => this.conn.Value.GetDatabase();

    public async Task StoreReadingAsync(Reading r, CancellationToken ct)
    {
        var key = $"readings:{r.SensorId}";
        var value = JsonSerializer.Serialize(r);
        await this.Db.ListRightPushAsync(key, value);
        await this.Db.KeyExpireAsync(key, Ttl);
    }

    public async Task StoreAggregateAsync(Processing.Models.Aggregate a, CancellationToken ct)
    {
        var key = $"aggregates:{a.SensorId}:{a.Window.ToString().ToLowerInvariant()}";
        var value = JsonSerializer.Serialize(a);
        await this.Db.ListRightPushAsync(key, value);
        await this.Db.KeyExpireAsync(key, Ttl);
    }

    public async Task StoreAlertAsync(Alert a, CancellationToken ct)
    {
        var key = "alerts";
        var value = JsonSerializer.Serialize(a);
        await this.Db.ListRightPushAsync(key, value);
        await this.Db.KeyExpireAsync(key, Ttl);
    }
}
