using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Exceptions;

/// <summary>
/// Thrown when a ride state transition is attempted that is not in the valid transition table.
/// </summary>
public sealed class InvalidRideTransitionException : Exception
{
    public RideStatus FromStatus { get; }
    public RideStatus ToStatus { get; }

    public InvalidRideTransitionException(RideStatus fromStatus, RideStatus toStatus)
        : base($"Cannot transition ride from {fromStatus} to {toStatus}.")
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}
