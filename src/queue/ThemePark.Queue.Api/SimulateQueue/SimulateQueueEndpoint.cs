using ThemePark.Queue.Api.Models;

namespace ThemePark.Queue.Api.SimulateQueue;

public static class SimulateQueueEndpoint
{
    /// <summary>
    /// Registers the simulate-queue endpoint only when <c>Dapr:DemoMode</c> is <c>true</c> (task 5.2).
    /// </summary>
    public static IEndpointRouteBuilder MapSimulateQueue(this IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var demoMode = configuration.GetValue<bool>("Dapr:DemoMode");
        if (!demoMode)
            return routes;

        routes.MapPost("/queue/{rideId}/simulate-queue",
            async (string rideId, SimulateQueueRequest request, SimulateQueueHandler handler, CancellationToken ct) =>
                await handler.HandleAsync(rideId, request, ct))
            .WithName("SimulateQueue")
            .Produces(StatusCodes.Status200OK);

        return routes;
    }
}
