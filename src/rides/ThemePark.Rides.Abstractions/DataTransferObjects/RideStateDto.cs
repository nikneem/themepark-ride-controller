namespace ThemePark.Rides.Abstractions.DataTransferObjects;

public sealed record RideStateDto(
    Guid RideId,
    string Name,
    string OperationalStatus,
    int Capacity,
    int CurrentPassengerCount,
    string? PauseReason);
