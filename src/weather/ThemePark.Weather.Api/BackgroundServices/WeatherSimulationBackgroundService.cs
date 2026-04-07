using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.Configuration;
using ThemePark.Weather.Api.Services;

namespace ThemePark.Weather.Api.BackgroundServices;

public sealed class WeatherSimulationBackgroundService : BackgroundService
{
    private readonly IWeatherSimulationEngine _engine;
    private readonly DaprClient _daprClient;
    private readonly int _intervalSeconds;
    private readonly ILogger<WeatherSimulationBackgroundService> _logger;

    public WeatherSimulationBackgroundService(
        IWeatherSimulationEngine engine,
        DaprClient daprClient,
        IConfiguration configuration,
        ILogger<WeatherSimulationBackgroundService> logger)
    {
        _engine = engine;
        _daprClient = daprClient;
        _intervalSeconds = configuration.GetValue<int>($"{WeatherOptions.SectionName}:SimulationIntervalSeconds", 60);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await TickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    /// <summary>
    /// Executes one simulation tick. Exposed internally for unit testing.
    /// </summary>
    public async Task TickAsync(CancellationToken ct = default)
    {
        var condition = _engine.GenerateCondition();
        _logger.LogInformation("Weather tick: {Severity} affecting zones [{Zones}]",
            condition.Severity, string.Join(", ", condition.AffectedZones));

        if (condition.Severity != WeatherSeverity.Calm)
        {
            var evt = new WeatherAlertEvent(
                Guid.NewGuid(),
                condition.Severity,
                condition.AffectedZones,
                condition.GeneratedAt);

            await _daprClient.PublishEventAsync("themepark-pubsub", "weather.alert", evt, ct);
        }
    }
}
