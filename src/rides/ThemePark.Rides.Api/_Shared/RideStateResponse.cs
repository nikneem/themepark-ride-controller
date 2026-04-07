using ThemePark.Rides.Models;

namespace ThemePark.Rides.Api._Shared;

/// <summary>API response DTO for ride state queries.</summary>
public sealed record RideStateResponse(
    Guid RideId,
    string Name,
    string OperationalStatus,
    int Capacity,
    int CurrentPassengerCount,
    string? PauseReason)
{
    public static RideStateResponse From(RideState state) => new(
        state.RideId,
        state.Name,
        state.OperationalStatus.ToString(),
        state.Capacity,
        state.CurrentPassengerCount,
        state.PauseReason);
}
