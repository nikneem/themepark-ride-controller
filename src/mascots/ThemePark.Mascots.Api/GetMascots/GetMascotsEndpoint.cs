namespace ThemePark.Mascots.Api.GetMascots;

public static class GetMascotsEndpoint
{
    public static IEndpointRouteBuilder MapGetMascots(this IEndpointRouteBuilder app)
    {
        app.MapGet("/mascots", (GetMascotsHandler handler) => Results.Ok(handler.Handle()));
        return app;
    }
}
