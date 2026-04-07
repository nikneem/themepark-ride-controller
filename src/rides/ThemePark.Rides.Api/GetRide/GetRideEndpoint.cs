namespace ThemePark.Rides.Api.GetRide;

public static class GetRideEndpoint
{
    public static IEndpointRouteBuilder MapGetRide(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/rides/{rideId}", async (string rideId, GetRideHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(rideId, ct))
            .WithName("GetRide")
            .Produces<ThemePark.Rides.Api._Shared.RideStateResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return routes;
    }
}
