namespace ThemePark.Mascots.Api.ClearMascot;

public static class ClearMascotEndpoint
{
    public static IEndpointRouteBuilder MapClearMascot(this IEndpointRouteBuilder app)
    {
        app.MapPost("/mascots/{mascotId}/clear",
            (string mascotId, ClearMascotHandler handler, CancellationToken ct) =>
                handler.HandleAsync(mascotId, ct));
        return app;
    }
}
