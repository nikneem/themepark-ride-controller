using Dapr.Client;
using Microsoft.Extensions.Configuration;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.EventContracts.Events;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Rides.Features.SimulateMalfunction;

/// <summary>
/// Demo-only handler: publishes a <c>ride.malfunction</c> pub/sub event.
/// Only active when <c>Dapr:DemoMode</c> configuration is <c>true</c>.
/// Returns NotFound when demo mode is disabled or the ride is not found.
/// </summary>
public sealed class SimulateMalfunctionHandler(
    IRideStateStore store,
    DaprClient daprClient,
    IConfiguration configuration)
    : ICommandHandler<SimulateMalfunctionCommand, OperationResult>
{
    private const string PubSubName = AspireConstants.DaprComponents.PubSub;
    private const string TopicName = "ride.malfunction";

    public async Task<OperationResult> HandleAsync(
        SimulateMalfunctionCommand command,
        CancellationToken cancellationToken = default)
    {
        var demoMode = configuration.GetValue<bool>("Dapr:DemoMode");
        if (!demoMode)
            return OperationResult.NotFound();

        var state = await store.GetAsync(command.RideId, cancellationToken);
        if (state is null)
            return OperationResult.NotFound();

        var evt = new RideMalfunctionEvent(
            EventId: Guid.NewGuid(),
            RideId: state.RideId,
            RideName: state.Name,
            FaultCode: "SimulatedMalfunction",
            Description: $"Simulated malfunction on {state.Name}",
            OccurredAt: DateTimeOffset.UtcNow);

        await daprClient.PublishEventAsync(PubSubName, TopicName, evt, cancellationToken);
        return OperationResult.Success();
    }
}
