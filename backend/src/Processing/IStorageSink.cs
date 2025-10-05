namespace Processing;

using Processing.Models;

public interface IStorageSink
{
    Task StoreReadingAsync(Reading r, CancellationToken ct);
    Task StoreAggregateAsync(Aggregate a, CancellationToken ct);
    Task StoreAlertAsync(Alert a, CancellationToken ct);
}
