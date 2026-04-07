using ThemePark.Rides.Features.SimulateMalfunction;
using ThemePark.Shared;

namespace ThemePark.Rides.Api.SimulateMalfunction;

public static class SimulateMalfunctionEndpoint
{
    public static IEndpointRouteBuilder MapSimulateMalfunction(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/simulate-malfunction",
            async (string rideId, SimulateMalfunctionHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new SimulateMalfunctionCommand(rideId), ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : Results.NotFound();
            })
            .WithName("SimulateMalfunction")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return routes;
    }
}
