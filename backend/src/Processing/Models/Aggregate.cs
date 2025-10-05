namespace Processing.Models;

public enum Window
{
    W1m,
    W5m,
    W15m,
    W1h,
    W24h,
}

public sealed class Aggregate
{
    public string? SensorId { get; init; }
    public required Window Window { get; init; }
    public long Count { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double Mean { get; init; }
    public double Stdev { get; init; }
    public double P95 { get; init; }
}
