using Dapr.Client;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.EventContracts.Events;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Models;
using ThemePark.Weather.Services;

namespace ThemePark.Weather.Features.SimulateWeather;

public sealed class SimulateWeatherHandler(IWeatherSimulationEngine engine, DaprClient daprClient)
    : ICommandHandler<SimulateWeatherCommand, OperationResult>
{
    public async Task<OperationResult> HandleAsync(
        SimulateWeatherCommand command,
        CancellationToken cancellationToken = default)
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

            await daprClient.PublishEventAsync(AspireConstants.DaprComponents.PubSub, "weather.alert", evt, cancellationToken);
        }

        return OperationResult.Success();
    }
}
