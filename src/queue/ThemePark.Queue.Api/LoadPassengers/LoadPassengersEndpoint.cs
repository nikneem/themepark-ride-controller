using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Features.LoadPassengers;
using ThemePark.Shared;

namespace ThemePark.Queue.Api.LoadPassengers;

public static class LoadPassengersEndpoint
{
    public static IEndpointRouteBuilder MapLoadPassengers(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/queue/{rideId}/load",
            async (string rideId, LoadPassengersRequest request, LoadPassengersHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new LoadPassengersCommand(rideId, request.Capacity), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.Conflict(new { error = result.Error });
            })
            .WithName("LoadPassengers")
            .Produces<LoadPassengersResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict);

        return routes;
    }
}

