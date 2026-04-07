namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Maintenance Service on topic "maintenance.completed" when a maintenance job finishes.
/// </summary>
public sealed record MaintenanceCompletedEvent(
    Guid EventId,
    string MaintenanceId,
    Guid RideId,
    DateTimeOffset CompletedAt);
