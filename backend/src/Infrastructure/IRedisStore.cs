using Processing.Models;

namespace Infrastructure;

public interface IRedisStore
{
    Task StoreReadingAsync(Reading r, CancellationToken ct);
    Task StoreAggregateAsync(Aggregate a, CancellationToken ct);
    Task StoreAlertAsync(Alert a, CancellationToken ct);
}
