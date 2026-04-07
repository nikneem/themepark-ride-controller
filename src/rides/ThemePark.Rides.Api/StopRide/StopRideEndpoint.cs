using ThemePark.Rides.Features.StopRide;
using ThemePark.Shared;

namespace ThemePark.Rides.Api.StopRide;

public static class StopRideEndpoint
{
    public static IEndpointRouteBuilder MapStopRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/stop", async (string rideId, StopRideHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new StopRideCommand(rideId), ct);
            return result.IsSuccess
                ? Results.Ok()
                : result.ErrorKind == OperationErrorKind.NotFound
                    ? Results.NotFound()
                    : Results.Conflict(new { error = result.Error });
        })
        .WithName("StopRide")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}

