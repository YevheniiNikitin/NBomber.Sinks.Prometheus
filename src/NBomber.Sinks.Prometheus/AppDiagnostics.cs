using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace NBomber.Sinks.Prometheus;

internal sealed class AppDiagnostics
{
    internal static readonly string AssemblyVersion =
        typeof(AppDiagnostics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppDiagnostics).Assembly.GetName().Version!.ToString();

    internal static readonly Meter Meter = new("NBomber.PrometheusSink", AssemblyVersion);

    internal static readonly SynchronousGauge<int> NodeCount = new(Meter, "cluster.node_count", description: "Number of nodes involved in the test run");
    internal static readonly SynchronousGauge<int> CpuCount = new(Meter, "cluster.node_cpu_count", description: "Number of CPU cores involved in the test run");

    internal static readonly Histogram<double> SuccessfulRequestLatency = Meter.CreateHistogram<double>("ok.request.latency", description: "Latency for successful requests", unit: "ms");
    internal static readonly Histogram<double> FailedRequestLatency = Meter.CreateHistogram<double>("fail.request.latency", description: "Latency for failed requests", unit: "ms");


    internal static void SetUsersCount(double value, params KeyValuePair<string, object?>[]? tags)
    {
        const string name = "users.count";

        Gauge(name, null, null, value, tags);
    }

    internal static void SetTotalRps(double value, params KeyValuePair<string, object?>[]? tags)
    {
        const string name = "all.request.rps";
        const string unit = "req/s";
        const string description = "Number of requests per second for the step";

        Gauge(name, unit, description, value, tags);
    }

    internal static void SetSuccessfulRps(double value, params KeyValuePair<string, object?>[]? tags)
    {
        const string name = "ok.request.rps";
        const string unit = "req/s";
        const string description = "Number of successful requests per second for the step";

        Gauge(name, unit, description, value, tags);
    }

    internal static void SetFailedRps(double value, params KeyValuePair<string, object?>[]? tags)
    {
        const string name = "fail.request.rps";
        const string unit = "req/s";
        const string description = "Number of failed requests per second for a step";

        Gauge(name, unit, description, value, tags);
    }

    internal static void SetTotalRequestsCount(double value, params KeyValuePair<string, object?>[]? tags)
    {
        const string name = "all.request.count";

        Gauge(name, null, null, value, tags);
    }

    internal static void SetSuccessfulRequestsCount(double value, KeyValuePair<string, object?>[] tags)
    {
        const string name = "ok.request.count";

        Gauge(name, null, null, value, tags);
    }

    internal static void SetFailedRequestsCount(double value, KeyValuePair<string, object?>[] tags)
    {
        const string name = "fail.request.count";

        Gauge(name, null, null, value, tags);
    }


    private static readonly ConcurrentDictionary<(string name, KeyValuePair<string, object?>[] tags), SynchronousGauge<double>> Gauges =
        new(new GaugeComparer());

    private static void Gauge(string name, string? unit, string? description, double value, params KeyValuePair<string, object?>[]? tags)
    {
        tags ??= Array.Empty<KeyValuePair<string, object?>>();

        if (!Gauges.TryGetValue((name, tags), out var gauge))
            gauge = Gauges.GetOrAdd((name, tags), key => new SynchronousGauge<double>(Meter, name, unit, description));

        gauge.Set(value, tags);
    }
}
