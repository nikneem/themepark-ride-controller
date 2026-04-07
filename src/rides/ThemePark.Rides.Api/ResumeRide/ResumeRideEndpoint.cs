using ThemePark.Rides.Features.ResumeRide;
using ThemePark.Shared;

namespace ThemePark.Rides.Api.ResumeRide;

public static class ResumeRideEndpoint
{
    public static IEndpointRouteBuilder MapResumeRide(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/rides/{rideId}/resume", async (string rideId, ResumeRideHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(rideId, ct);
            return result.IsSuccess
                ? Results.Ok()
                : result.ErrorKind == OperationErrorKind.NotFound
                    ? Results.NotFound()
                    : Results.Conflict(new { error = result.Error });
        })
        .WithName("ResumeRide")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}

