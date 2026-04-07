using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Features.GetMascots;

namespace ThemePark.Mascots.Api.GetMascots;

public static class GetMascotsEndpoint
{
    public static IEndpointRouteBuilder MapGetMascots(this IEndpointRouteBuilder app)
    {
        app.MapGet("/mascots", (GetMascotsHandler handler) =>
        {
            var result = handler.Handle();
            return Results.Ok(result.Value);
        })
        .Produces<IReadOnlyList<MascotDto>>(StatusCodes.Status200OK);
        return app;
    }
}

