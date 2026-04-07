using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.Services;
using ThemePark.Weather.Models;

namespace ThemePark.Weather.Api.SimulateWeather;

public sealed class SimulateWeatherHandler(IWeatherSimulationEngine engine, DaprClient daprClient)
{
    public async Task<Results<Accepted, BadRequest<string>>> HandleAsync(
        SimulateWeatherRequest request,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<WeatherSeverity>(request.Severity, ignoreCase: true, out var severity))
            return TypedResults.BadRequest($"Invalid severity '{request.Severity}'. Use Calm, Mild, or Severe.");

        var condition = new WeatherCondition(severity, request.AffectedZones, DateTimeOffset.UtcNow);
        engine.ForceCondition(condition);

        if (severity != WeatherSeverity.Calm)
        {
            var evt = new WeatherAlertEvent(
                Guid.NewGuid(),
                severity,
                request.AffectedZones,
                condition.GeneratedAt);

            await daprClient.PublishEventAsync("themepark-pubsub", "weather.alert", evt, ct);
        }

        return TypedResults.Accepted((string?)null);
    }
}
