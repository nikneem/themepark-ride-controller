namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Maintenance Service on topic "maintenance.requested" when a maintenance job is created.
/// </summary>
public sealed record MaintenanceRequestedEvent(
    Guid EventId,
    string MaintenanceId,
    Guid RideId,
    string Reason,
    DateTimeOffset RequestedAt);
