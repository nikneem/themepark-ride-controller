using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Events;

/// <summary>
/// Domain event raised by <see cref="StateMachine.RideStateMachine"/> on every valid state transition.
/// </summary>
public sealed record RideStatusChanged(
    string RideId,
    RideStatus PreviousStatus,
    RideStatus NewStatus,
    DateTimeOffset TransitionedAt);
