namespace ThemePark.Rides.Api.StopRide;

public static class StopRideEndpoint
{
    public static IEndpointRouteBuilder MapStopRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/stop", async (string rideId, StopRideHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(rideId, ct))
            .WithName("StopRide")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}
