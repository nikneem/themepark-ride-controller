using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.PubSub;

/// <summary>
/// Publishes <see cref="RideStatusChangedEvent"/> to topic <c>ride.status-changed</c>
/// on the <c>themepark-pubsub</c> Dapr pub/sub component.
/// </summary>
public sealed class RideStatusEventPublisher(DaprClient daprClient) : IRideStatusEventPublisher
{
    private const string PubSubName = "themepark-pubsub";
    private const string TopicName = "ride.status-changed";

    public Task PublishAsync(
        string rideId,
        RideStatus previousStatus,
        RideStatus newStatus,
        string? workflowStep,
        CancellationToken cancellationToken = default)
    {
        var evt = new RideStatusChangedEvent(
            RideId: Guid.TryParse(rideId, out var guid) ? guid : Guid.Empty,
            PreviousStatus: previousStatus.ToString(),
            NewStatus: newStatus.ToString(),
            WorkflowStep: workflowStep ?? string.Empty,
            ChangedAt: DateTimeOffset.UtcNow);

        return daprClient.PublishEventAsync(PubSubName, TopicName, evt, cancellationToken);
    }
}
