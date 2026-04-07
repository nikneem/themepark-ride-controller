using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;
using ThemePark.Weather.Api.Services;
using ThemePark.Weather.Models;

namespace ThemePark.Weather.Api.SimulateWeather;

public sealed record SimulateWeatherRequest(string Severity, string[] AffectedZones);

public static class SimulateWeatherEndpoint
{
    public static void Map(WebApplication app)
    {
        var isDemoMode = app.Configuration.GetValue<bool>("Dapr:DemoMode");
        if (!isDemoMode) return;

        app.MapPost("/weather/simulate", async (
            SimulateWeatherRequest request,
            IWeatherSimulationEngine engine,
            DaprClient daprClient,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<WeatherSeverity>(request.Severity, ignoreCase: true, out var severity))
                return Results.BadRequest($"Invalid severity '{request.Severity}'. Use Calm, Mild, or Severe.");

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

            return Results.Accepted();
        })
        .WithName("SimulateWeather")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
