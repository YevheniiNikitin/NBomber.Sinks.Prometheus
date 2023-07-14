using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace NBomber.Sinks.Prometheus;

public sealed class PrometheusSink : IReportingSink
{
    public string SinkName => "NBomber.Sinks.Prometheus";


    public Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        _context = context;

        var config = new PrometheusSinkConfig();
        if (infraConfig.GetSection(nameof(PrometheusSink)).Exists())
        {
            BindConfigurationSection(infraConfig, config);
        }

        var httpListenerAddresses =
            string.Join(", ", config.HttpListenerPrefixes.Select(prefix => $"{prefix}{config.ScrapeEndpointPath}"));

        _context.Logger.Information(
            "{SinkName} listens at those addresses: {HttpListenerPrefixes}",
            SinkName,
            httpListenerAddresses);

        _customTags = config.CustomTags.Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value)).ToArray();

        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(AppDiagnostics.Meter.Name)
            .AddPrometheusHttpListener(options =>
            {
                options.ScrapeEndpointPath = config.ScrapeEndpointPath;
                options.UriPrefixes = config.HttpListenerPrefixes;
            })
            .Build();

        return Task.CompletedTask;
    }

    public Task Start()
    {
        var testInfoTags = GetTestInfoTags();

        AppDiagnostics.NodeCount.Set(1, testInfoTags);
        AppDiagnostics.CpuCount.Set(_context.GetNodeInfo().CoresCount, testInfoTags);

        return Task.CompletedTask;
    }

    public Task SaveRealtimeStats(ScenarioStats[] stats) =>
        SaveScenarioStats(stats);

    public Task SaveFinalStats(NodeStats stats) =>
        SaveScenarioStats(stats.ScenarioStats);

    public Task Stop()
    {
        _meterProvider?.ForceFlush();
        _meterProvider?.Shutdown();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _meterProvider?.Dispose();
    }


    private static void BindConfigurationSection(IConfiguration infraConfig, PrometheusSinkConfig config)
    {
        var httpListenerPrefixesExists = infraConfig
            .GetSection(nameof(PrometheusSink))
            .GetSection("HttpListenerPrefixes")
            .Exists();

        if (httpListenerPrefixesExists)
        {
            config.HttpListenerPrefixes = null!;
        }

        infraConfig.GetSection(nameof(PrometheusSink)).Bind(config);
    }

    private Task SaveScenarioStats(ScenarioStats[] stats)
    {
        var statsSpan = stats.AsSpan();
        for (int i = 0; i < statsSpan.Length; i++)
        {
            var stat = statsSpan[i];
            MapStats(stat);
        }

        return Task.CompletedTask;
    }

    private void MapStats(ScenarioStats scenarioStats)
    {
        if (scenarioStats.StepStats.Length == 0)
        {
            RecordScenarioStats(scenarioStats);
            return;
        }

        RecordStepStats(scenarioStats);
    }

    private void RecordScenarioStats(ScenarioStats scenarioStats)
    {
        var okStats = scenarioStats.Ok;
        var failStats = scenarioStats.Fail;

        RecordStats(scenarioStats, okStats, failStats);
    }

    private void RecordStepStats(ScenarioStats scenarioStats)
    {
        var stepStats = scenarioStats.StepStats.AsSpan();
        for (int i = 0; i < stepStats.Length; i++)
        {
            var step = stepStats[i];

            var okStats = step.Ok;
            var failStats = step.Fail;

            RecordStats(scenarioStats, okStats, failStats, step);
        }
    }

    private void RecordStats(
        ScenarioStats scenarioStats,
        MeasurementStats okStats,
        MeasurementStats failStats,
        StepStats? step = null)
    {
        var allTags = GetTags(scenarioStats, step);

        AppDiagnostics.SetUsersCount(scenarioStats.LoadSimulationStats.Value, allTags);

        AppDiagnostics.SetTotalRps(okStats.Request.RPS + failStats.Request.RPS, allTags);
        AppDiagnostics.SetSuccessfulRps(okStats.Request.RPS, allTags);
        AppDiagnostics.SetFailedRps(failStats.Request.RPS, allTags);

        AppDiagnostics.SetTotalRequestsCount(okStats.Request.Count + failStats.Request.Count, allTags);
        AppDiagnostics.SetSuccessfulRequestsCount(okStats.Request.Count, allTags);
        AppDiagnostics.SetFailedRequestsCount(failStats.Request.Count, allTags);

        // looks bad, 'cause we're copying data from F# Histogram to C# Histogram
        AppDiagnostics.SuccessfulRequestLatency.Record(okStats.Latency.Percent50, allTags);
        AppDiagnostics.SuccessfulRequestLatency.Record(okStats.Latency.Percent75, allTags);
        AppDiagnostics.SuccessfulRequestLatency.Record(okStats.Latency.Percent95, allTags);
        AppDiagnostics.SuccessfulRequestLatency.Record(okStats.Latency.Percent99, allTags);

        AppDiagnostics.FailedRequestLatency.Record(failStats.Latency.Percent50, allTags);
        AppDiagnostics.FailedRequestLatency.Record(failStats.Latency.Percent75, allTags);
        AppDiagnostics.FailedRequestLatency.Record(failStats.Latency.Percent95, allTags);
        AppDiagnostics.FailedRequestLatency.Record(failStats.Latency.Percent99, allTags);
    }

    private KeyValuePair<string, object?>[] GetCompleteArrayOfTestInfoTags()
    {
        var nodeInfo = _context.GetNodeInfo();
        var testInfo = _context.TestInfo;

        var testInfoTags = new KeyValuePair<string, object?>[TestInfoTagsLength + AdditionalTagsLength + _customTags.Length];
        testInfoTags[0] = new KeyValuePair<string, object?>("session_id", testInfo.SessionId);
        testInfoTags[1] = new KeyValuePair<string, object?>("current_operation", nodeInfo.CurrentOperation.ToString().ToLower());
        testInfoTags[2] = new KeyValuePair<string, object?>("node_type", nodeInfo.NodeType.ToString());
        testInfoTags[3] = new KeyValuePair<string, object?>("test_suite", testInfo.TestSuite);
        testInfoTags[4] = new KeyValuePair<string, object?>("test_name", testInfo.TestName);
        testInfoTags[5] = new KeyValuePair<string, object?>("cluster_id", testInfo.ClusterId);

        _customTags.AsSpan().CopyTo(((Span<KeyValuePair<string, object?>>)testInfoTags)[TestInfoTagsLength..]);

        return testInfoTags;
    }

    private KeyValuePair<string, object?>[] GetTestInfoTags() =>
        GetCompleteArrayOfTestInfoTags()[..^2];

    private KeyValuePair<string, object?>[] GetTags(ScenarioStats scenarioStats, StepStats? step = null)
    {
        KeyValuePair<string, object?>[] testInfoTags = GetCompleteArrayOfTestInfoTags();

        testInfoTags[TestInfoTagsLength + _customTags.Length] =
            new KeyValuePair<string, object?>("scenario_name", scenarioStats.ScenarioName);

        if (step is null)
        {
            return testInfoTags[..^1];
        }

        testInfoTags[TestInfoTagsLength + _customTags.Length + 1] =
            new KeyValuePair<string, object?>("step_name", step.StepName);

        return testInfoTags;
    }


    private KeyValuePair<string, object?>[] _customTags = null!;
    private IBaseContext _context = null!;
    private MeterProvider? _meterProvider;


    private const int TestInfoTagsLength = 6;
    private const int AdditionalTagsLength = 2; // scenario_name and step_name
}
