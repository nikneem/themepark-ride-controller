namespace ThemePark.Queue.Api.GetQueue;

public static class GetQueueEndpoint
{
    public static IEndpointRouteBuilder MapGetQueue(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/queue/{rideId}", async (string rideId, GetQueueHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(rideId, ct))
            .WithName("GetQueue")
            .Produces<Models.QueueStateResponse>(StatusCodes.Status200OK);

        return routes;
    }
}
