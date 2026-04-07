using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Models;

/// <summary>
/// Full operational state of a single ride persisted in the Dapr state store.
/// Key format: <c>ride-state-{rideId}</c>.
/// </summary>
public sealed record RideState(
    Guid RideId,
    string Name,
    RideStatus OperationalStatus,
    int Capacity,
    int CurrentPassengerCount,
    string? PauseReason)
{
    /// <summary>Returns a copy with the <see cref="OperationalStatus"/> changed to <paramref name="status"/>.</summary>
    public RideState WithStatus(RideStatus status) => this with { OperationalStatus = status, PauseReason = null };

    /// <summary>Returns a copy paused with the given reason.</summary>
    public RideState WithPause(string reason) => this with { OperationalStatus = RideStatus.Paused, PauseReason = reason };
}
