namespace ThemePark.Rides.Api.StartRide;

public static class StartRideEndpoint
{
    public static IEndpointRouteBuilder MapStartRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/start", async (string rideId, StartRideHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(rideId, ct))
            .WithName("StartRide")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}
