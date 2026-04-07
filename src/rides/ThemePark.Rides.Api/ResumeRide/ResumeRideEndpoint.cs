namespace ThemePark.Rides.Api.ResumeRide;

public static class ResumeRideEndpoint
{
    public static IEndpointRouteBuilder MapResumeRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/resume", async (string rideId, ResumeRideHandler handler, CancellationToken ct) =>
            await handler.HandleAsync(rideId, ct))
            .WithName("ResumeRide")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}
