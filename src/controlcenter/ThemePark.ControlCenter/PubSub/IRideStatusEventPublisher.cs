using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.PubSub;

/// <summary>Publishes <see cref="RideStatusChangedEvent"/> to the Dapr pub/sub broker.</summary>
public interface IRideStatusEventPublisher
{
    Task PublishAsync(
        string rideId,
        RideStatus previousStatus,
        RideStatus newStatus,
        string? workflowStep,
        CancellationToken cancellationToken = default);
}
