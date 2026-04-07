namespace ThemePark.Rides.Api.PauseRide;

public static class PauseRideEndpoint
{
    public static IEndpointRouteBuilder MapPauseRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/pause", async (string rideId, PauseRideRequest request, PauseRideHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(rideId, request, ct))
            .WithName("PauseRide")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}
