using ThemePark.Rides.Features.PauseRide;
using ThemePark.Shared;

namespace ThemePark.Rides.Api.PauseRide;

public static class PauseRideEndpoint
{
    public static IEndpointRouteBuilder MapPauseRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/pause", async (string rideId, PauseRideRequest request, PauseRideHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(rideId, new PauseRideCommand(request.Reason), ct);
            return result.IsSuccess
                ? Results.Ok()
                : result.ErrorKind == OperationErrorKind.NotFound
                    ? Results.NotFound()
                    : result.ErrorKind == OperationErrorKind.BadRequest
                        ? Results.BadRequest(new { error = result.Error })
                        : Results.Conflict(new { error = result.Error });
        })
        .WithName("PauseRide")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}

