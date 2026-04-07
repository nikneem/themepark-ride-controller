using ThemePark.Rides.Events;
using ThemePark.Rides.Exceptions;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.StateMachine;

/// <summary>
/// Enforces valid <see cref="RideStatus"/> transitions for a single ride session.
/// Raises <see cref="RideStatusChanged"/> domain events on every valid transition.
/// </summary>
public sealed class RideStateMachine
{
    // Static lookup table of all valid transitions.
    private static readonly IReadOnlyDictionary<RideStatus, IReadOnlySet<RideStatus>> ValidTransitions =
        new Dictionary<RideStatus, IReadOnlySet<RideStatus>>
        {
            [RideStatus.Idle]        = new HashSet<RideStatus> { RideStatus.PreFlight },
            [RideStatus.PreFlight]   = new HashSet<RideStatus> { RideStatus.Loading, RideStatus.Failed },
            [RideStatus.Loading]     = new HashSet<RideStatus> { RideStatus.Running },
            [RideStatus.Running]     = new HashSet<RideStatus> { RideStatus.Paused, RideStatus.Maintenance, RideStatus.Completed, RideStatus.Failed },
            [RideStatus.Paused]      = new HashSet<RideStatus> { RideStatus.Running, RideStatus.Failed },
            [RideStatus.Maintenance] = new HashSet<RideStatus> { RideStatus.Resuming, RideStatus.Failed },
            [RideStatus.Resuming]    = new HashSet<RideStatus> { RideStatus.Running, RideStatus.Failed },
            [RideStatus.Completed]   = new HashSet<RideStatus> { RideStatus.Idle },
            [RideStatus.Failed]      = new HashSet<RideStatus> { RideStatus.Idle },
        };

    private readonly string _rideId;
    private readonly List<RideStatusChanged> _domainEvents = [];

    public RideStatus CurrentStatus { get; private set; }

    /// <summary>Collected domain events since the last <see cref="ClearEvents"/> call.</summary>
    public IReadOnlyList<RideStatusChanged> DomainEvents => _domainEvents;

    public RideStateMachine(string rideId, RideStatus initialStatus = RideStatus.Idle)
    {
        _rideId = rideId;
        CurrentStatus = initialStatus;
    }

    /// <summary>
    /// Transitions the ride to <paramref name="target"/>.
    /// Throws <see cref="InvalidRideTransitionException"/> if the transition is not allowed.
    /// </summary>
    public void Transition(RideStatus target)
    {
        if (!ValidTransitions.TryGetValue(CurrentStatus, out var allowed) || !allowed.Contains(target))
            throw new InvalidRideTransitionException(CurrentStatus, target);

        var previous = CurrentStatus;
        CurrentStatus = target;

        _domainEvents.Add(new RideStatusChanged(
            RideId: _rideId,
            PreviousStatus: previous,
            NewStatus: target,
            TransitionedAt: DateTimeOffset.UtcNow));
    }

    /// <summary>Clears all collected domain events after they have been dispatched.</summary>
    public void ClearEvents() => _domainEvents.Clear();
}
