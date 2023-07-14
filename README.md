# NBomber Prometheus Sink

NBomber Prometheus Sink is a custom sink for NBomber load-testing framework. It integrates with Prometheus, a popular monitoring and alerting toolkit, allowing you to collect and visualize load testing metrics.

## Features

- Integrates NBomber with Prometheus for monitoring load test metrics.
- Provides predefined metrics for request latency, request counts, RPS, and more.
- Supports custom tags for fine-grained metric grouping.
- Easy setup and configuration.

### Installation

You can install the NBomber Prometheus Sink via NuGet. Run the following command in the NuGet Package Manager Console:
```code
PM> Install-Package NBomber.Sinks.Prometheus
```

### Usage

To use the NBomber Prometheus Sink, follow these steps:

1. Set up your load test scenario using NBomber.

2. Configure NBomber to use the Prometheus sink. Refer to the NBomber documentation for information on how to configure sinks.

3. Configure Prometheus job to scrape metrics from the NBomber Prometheus sink.

4. Run your load test.

For more details on configuring and using the NBomber Prometheus Sink, refer to the [samples](link-to-documentation).

## Code Samples

Here's an example of how to set up a load test scenario with the NBomber Prometheus Sink:

```csharp
// Create a Prometheus Sink
var prometheusSink = new PrometheusSink();

// Configure your scenario
var scenario = Scenario.Create("MyScenario", RadclientAuthenticateUser);

// Start the load test
NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportingInterval(TimeSpan.FromSeconds(10)) // Default OpenTelemetry exporter reporting interval
    .WithReportingSinks(prometheusSink)
    .Run()
```

For more code samples and examples, please refer to the [samples](link-to-documentation) directory.

# How it works

NBomber.Sinks.Prometheus utilizes [OpenTelemetry.Exporter.Prometheus.HttpListener](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener) to export metrics.
During the execution of your load test, the sink creates an HttpListener instance that listens on the `http://localhost:9464/metrics` endpoint by default.
Subsequently, the Prometheus job scrapes metrics by calling the endpoint.