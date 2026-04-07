using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Rides.Api._Shared;

namespace ThemePark.Rides.Api.SimulateMalfunction;

/// <summary>
/// Demo-only endpoint: publishes a <c>ride.malfunction</c> pub/sub event.
/// Only active when <c>Dapr:DemoMode</c> configuration is <c>true</c>.
/// Returns 404 when demo mode is disabled or the ride is not found.
/// </summary>
public sealed class SimulateMalfunctionHandler(IRideStateStore store, DaprClient daprClient, IConfiguration configuration)
{
    private const string PubSubName = "themepark-pubsub";
    private const string TopicName = "ride.malfunction";

    public async Task<IResult> HandleAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var demoMode = configuration.GetValue<bool>("Dapr:DemoMode");
        if (!demoMode)
            return Results.NotFound();

        var state = await store.GetAsync(rideId, cancellationToken);
        if (state is null)
            return Results.NotFound();

        var evt = new RideMalfunctionEvent(
            EventId: Guid.NewGuid(),
            RideId: state.RideId,
            RideName: state.Name,
            FaultCode: "SimulatedMalfunction",
            Description: $"Simulated malfunction on {state.Name}",
            OccurredAt: DateTimeOffset.UtcNow);

        await daprClient.PublishEventAsync(PubSubName, TopicName, evt, cancellationToken);
        return Results.Ok();
    }
}
