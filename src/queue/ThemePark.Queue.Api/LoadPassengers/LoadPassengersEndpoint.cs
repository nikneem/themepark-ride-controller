using ThemePark.Queue.Api.Models;

namespace ThemePark.Queue.Api.LoadPassengers;

public static class LoadPassengersEndpoint
{
    public static IEndpointRouteBuilder MapLoadPassengers(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/queue/{rideId}/load",
            async (string rideId, LoadPassengersRequest request, LoadPassengersHandler handler, CancellationToken ct) =>
                await handler.HandleAsync(rideId, request, ct))
            .WithName("LoadPassengers")
            .Produces<LoadPassengersResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}
