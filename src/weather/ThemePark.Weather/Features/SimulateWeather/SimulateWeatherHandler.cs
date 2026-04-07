using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Shared;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Models;
using ThemePark.Weather.Services;

namespace ThemePark.Weather.Features.SimulateWeather;

public sealed record SimulateWeatherCommand(string Severity, string[] AffectedZones);

public sealed class SimulateWeatherHandler(IWeatherSimulationEngine engine, DaprClient daprClient)
{
    public async Task<OperationResult> HandleAsync(SimulateWeatherCommand command, CancellationToken ct = default)
    {
        if (!Enum.TryParse<WeatherSeverity>(command.Severity, ignoreCase: true, out var severity))
            return OperationResult.BadRequest($"Invalid severity '{command.Severity}'. Use Calm, Mild, or Severe.");

        var condition = new WeatherCondition(severity, command.AffectedZones, DateTimeOffset.UtcNow);
        engine.ForceCondition(condition);

        if (severity != WeatherSeverity.Calm)
        {
            var evt = new WeatherAlertEvent(
                Guid.NewGuid(),
                severity,
                command.AffectedZones,
                condition.GeneratedAt);

            await daprClient.PublishEventAsync("themepark-pubsub", "weather.alert", evt, ct);
        }

        return OperationResult.Success();
    }
}
