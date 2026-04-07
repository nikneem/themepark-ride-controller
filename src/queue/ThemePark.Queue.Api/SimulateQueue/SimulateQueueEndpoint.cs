using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Features.SimulateQueue;

namespace ThemePark.Queue.Api.SimulateQueue;

public static class SimulateQueueEndpoint
{
    public static IEndpointRouteBuilder MapSimulateQueue(this IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var demoMode = configuration.GetValue<bool>("Dapr:DemoMode");
        if (!demoMode)
            return routes;

        routes.MapPost("/queue/{rideId}/simulate-queue",
            async (string rideId, SimulateQueueRequest request, SimulateQueueHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new SimulateQueueCommand(rideId, request.Count, request.VipProbability), ct);
                return Results.Ok(result.Value);
            })
            .WithName("SimulateQueue")
            .Produces(StatusCodes.Status200OK);

        return routes;
    }
}

