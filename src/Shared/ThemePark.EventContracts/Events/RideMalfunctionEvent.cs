namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Ride Service on topic "ride.malfunction" when a fault is detected.
/// </summary>
public sealed record RideMalfunctionEvent(
    Guid EventId,
    Guid RideId,
    string FaultCode,
    string Description,
    DateTimeOffset OccurredAt);
