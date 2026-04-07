namespace ThemePark.EventContracts.Events;

/// <summary>
/// Published by Control Center API (RideWorkflow) on topic "ride.status-changed" on every ride state transition.
/// Forwarded to frontend clients via SSE.
/// </summary>
public sealed record RideStatusChangedEvent(
    Guid RideId,
    string PreviousStatus,
    string NewStatus,
    string WorkflowStep,
    DateTimeOffset ChangedAt);
