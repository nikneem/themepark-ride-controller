namespace ThemePark.Rides.Api.SimulateMalfunction;

public static class SimulateMalfunctionEndpoint
{
    public static IEndpointRouteBuilder MapSimulateMalfunction(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/simulate-malfunction",
            async (string rideId, SimulateMalfunctionHandler handler, CancellationToken ct) =>
                await handler.HandleAsync(rideId, ct))
            .WithName("SimulateMalfunction")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return routes;
    }
}
