using System.Diagnostics.Metrics;

namespace NBomber.Sinks.Prometheus;

// TODO: remove it when a synchronous gauge will be implemented
// Workarounds to make ObservableGauges synchronous
public sealed class SynchronousGauge<T> where T : struct
{
#pragma warning disable IDE0052 // Remove unread private members
    private readonly ObservableGauge<T> _gauge;
#pragma warning restore IDE0052 // Remove unread private members

    public T Value { get; private set; }
    public KeyValuePair<string, object?>[]? Tags { get; private set; }

    public SynchronousGauge(Meter meter, string name, string? unit = null, string? description = null)
    {
        _gauge = meter.CreateObservableGauge(name, () => new Measurement<T>(Value, Tags), unit, description);
    }

    public void Set(T value, params KeyValuePair<string, object?>[]? tags)
    {
        Value = value;
        Tags = tags ?? Array.Empty<KeyValuePair<string, object?>>();
    }
}
