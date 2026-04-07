using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Features.GetQueue;
using ThemePark.Shared;

namespace ThemePark.Queue.Api.GetQueue;

public static class GetQueueEndpoint
{
    public static IEndpointRouteBuilder MapGetQueue(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/queue/{rideId}", async (string rideId, GetQueueHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(rideId, ct);
            return Results.Ok(result.Value);
        })
        .WithName("GetQueue")
        .Produces<QueueStateResponse>(StatusCodes.Status200OK);

        return routes;
    }
}

