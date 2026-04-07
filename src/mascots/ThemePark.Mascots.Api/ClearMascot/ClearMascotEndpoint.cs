using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Features.ClearMascot;
using ThemePark.Shared;

namespace ThemePark.Mascots.Api.ClearMascot;

public static class ClearMascotEndpoint
{
    public static IEndpointRouteBuilder MapClearMascot(this IEndpointRouteBuilder app)
    {
        app.MapPost("/mascots/{mascotId}/clear",
            async (string mascotId, ClearMascotHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(mascotId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound();
            })
            .Produces<ClearMascotResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        return app;
    }
}

