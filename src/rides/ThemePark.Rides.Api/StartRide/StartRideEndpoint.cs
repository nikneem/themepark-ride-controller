using ThemePark.Rides.Features.StartRide;
using ThemePark.Shared;

namespace ThemePark.Rides.Api.StartRide;

public static class StartRideEndpoint
{
    public static IEndpointRouteBuilder MapStartRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/start", async (string rideId, StartRideHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new StartRideCommand(rideId), ct);
            return result.IsSuccess
                ? Results.Ok()
                : result.ErrorKind == OperationErrorKind.NotFound
                    ? Results.NotFound()
                    : Results.Conflict(new { error = result.Error });
        })
        .WithName("StartRide")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}

