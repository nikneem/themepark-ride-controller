namespace ThemePark.Maintenance.Abstractions.DataTransferObjects;

public sealed record CreateMaintenanceRequestCommand(
    Guid RideId,
    string Reason,
    string? WorkflowId,
    DateTimeOffset RequestedAt);

public sealed record CreateMaintenanceRequestResponse(
    Guid MaintenanceId,
    Guid RideId,
    string Status);

public sealed record CompleteMaintenanceRequestCommand(Guid MaintenanceId);

public sealed record CompleteMaintenanceRequestResponse(
    Guid MaintenanceId,
    Guid RideId,
    string Status,
    int? DurationMinutes);

public sealed record MaintenanceHistoryItem(
    Guid MaintenanceId,
    Guid RideId,
    string Reason,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes);

public sealed record GetMaintenanceHistoryResponse(Guid RideId, IReadOnlyList<MaintenanceHistoryItem> History);
