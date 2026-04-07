using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Features.GetMascots;

namespace ThemePark.Mascots.Api.GetMascots;

public static class GetMascotsEndpoint
{
    public static IEndpointRouteBuilder MapGetMascots(this IEndpointRouteBuilder app)
    {
        app.MapGet("/mascots", async (GetMascotsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetMascotsQuery(), ct);
            return Results.Ok(result.Value);
        })
        .Produces<IReadOnlyList<MascotDto>>(StatusCodes.Status200OK);
        return app;
    }
}

