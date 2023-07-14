using NBomber.CSharp;
using NBomber.Sinks.Prometheus;

var scenario = Scenario.Create("hello_world_scenario", async context =>
{
    await Task.Delay(1000);

    return Response.Ok();
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.RampingInject(rate: 150, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
    Simulation.Inject(rate: 150, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportingInterval(TimeSpan.FromSeconds(5))
    .WithReportingSinks(new PrometheusSink())
    .LoadInfraConfig("infra-config.json")
    .Run();