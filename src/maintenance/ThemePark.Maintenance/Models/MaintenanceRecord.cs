using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Models;

public sealed record MaintenanceRecord(
    Guid MaintenanceId,
    Guid RideId,
    MaintenanceReason Reason,
    MaintenanceStatus Status,
    string? WorkflowId,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt)
{
    /// <summary>
    /// Duration in whole minutes between RequestedAt and CompletedAt. Null if not yet completed.
    /// </summary>
    public int? DurationMinutes =>
        CompletedAt.HasValue
            ? (int)(CompletedAt.Value - RequestedAt).TotalMinutes
            : null;

    public MaintenanceRecord WithStatus(MaintenanceStatus newStatus, DateTimeOffset? completedAt = null) =>
        this with { Status = newStatus, CompletedAt = completedAt ?? CompletedAt };
}
