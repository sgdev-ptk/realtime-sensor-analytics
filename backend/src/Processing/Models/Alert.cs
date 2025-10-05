namespace Processing.Models;

public enum Severity
{
    Info,
    Warn,
    Critical,
}

public sealed class Alert
{
    public required string Id { get; init; }
    public required string SensorId { get; init; }
    public required DateTimeOffset Ts { get; init; }
    public required string Type { get; init; }
    public required string Message { get; init; }
    public Severity Severity { get; init; }
    public bool Ack { get; set; }
}
