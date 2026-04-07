using ThemePark.Shared;
using ThemePark.Weather.Features.SimulateWeather;

namespace ThemePark.Weather.Api.SimulateWeather;

public static class SimulateWeatherEndpoint
{
    public static void Map(WebApplication app)
    {
        var isDemoMode = app.Configuration.GetValue<bool>("Dapr:DemoMode");
        if (!isDemoMode) return;

        app.MapPost("/weather/simulate", async (
            SimulateWeatherCommand command,
            SimulateWeatherHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(command, ct);
            return result.IsSuccess
                ? Results.Accepted()
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SimulateWeather")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest);
    }
}

