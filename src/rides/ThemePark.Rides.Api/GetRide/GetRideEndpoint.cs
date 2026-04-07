using ThemePark.Rides.Abstractions.DataTransferObjects;
using ThemePark.Rides.Features.GetRide;
using ThemePark.Shared;

namespace ThemePark.Rides.Api.GetRide;

public static class GetRideEndpoint
{
    public static IEndpointRouteBuilder MapGetRide(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/rides/{rideId}", async (string rideId, GetRideHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(rideId, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound();
        })
        .WithName("GetRide")
        .Produces<RideStateDto>()
        .Produces(StatusCodes.Status404NotFound);

        return routes;
    }
}

