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
            SimulateWeatherHandler handler,
            CancellationToken ct) => await handler.HandleAsync(request, ct))
        .WithName("SimulateWeather")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
