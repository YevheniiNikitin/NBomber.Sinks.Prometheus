namespace NBomber.Sinks.Prometheus;

public sealed record CustomTag
{
    public string Key { get; set; } = null!;
    public object? Value { get; set; }
}
