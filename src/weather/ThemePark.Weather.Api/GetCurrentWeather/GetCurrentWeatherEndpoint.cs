using ThemePark.Weather.Api.Services;
using ThemePark.Weather.Models;

namespace ThemePark.Weather.Api.GetCurrentWeather;

public static class GetCurrentWeatherEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/weather/current", (IWeatherSimulationEngine engine) =>
        {
            var condition = engine.CurrentCondition;
            return Results.Ok(new
            {
                severity = condition.Severity.ToString(),
                affectedZones = condition.AffectedZones,
                generatedAt = condition.GeneratedAt
            });
        })
        .WithName("GetCurrentWeather")
        .Produces<WeatherCondition>(StatusCodes.Status200OK);
    }
}
