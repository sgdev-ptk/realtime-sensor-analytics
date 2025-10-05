namespace Processing.Models;

public sealed class Reading
{
    public required string SensorId { get; init; }
    public required DateTimeOffset Ts { get; init; }
    public double Value { get; init; }
    public string Status { get; init; } = "ok";
}
