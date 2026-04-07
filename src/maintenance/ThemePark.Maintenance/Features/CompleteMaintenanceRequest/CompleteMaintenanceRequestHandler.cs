using Dapr.Client;
using System.Diagnostics;
using ThemePark.EventContracts.Events;
using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Features.CompleteMaintenanceRequest;

public sealed class CompleteMaintenanceRequestHandler(
    IMaintenanceStateStore stateStore,
    DaprClient daprClient)
{
    private static readonly ActivitySource ActivitySource = new("ThemePark.Maintenance");

    public async Task<OperationResult<CompleteMaintenanceRequestResponse>> HandleAsync(
        CompleteMaintenanceRequestCommand command,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("CompleteMaintenanceRequest");

        var record = await stateStore.GetRecordAsync(command.MaintenanceId, ct);
        if (record is null)
            return OperationResult<CompleteMaintenanceRequestResponse>.NotFound();

        if (record.Status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled)
            return OperationResult<CompleteMaintenanceRequestResponse>.Conflict(
                $"Maintenance record {command.MaintenanceId} is already in terminal state '{record.Status}'.");

        var completedAt = DateTimeOffset.UtcNow;
        var updated = record.WithStatus(MaintenanceStatus.Completed, completedAt);
        await stateStore.SaveRecordAsync(updated, ct);

        var evt = new MaintenanceCompletedEvent(
            Guid.NewGuid(),
            record.MaintenanceId.ToString(),
            record.RideId,
            completedAt);

        await daprClient.PublishEventAsync("themepark-pubsub", "maintenance.completed", evt, ct);

        activity?.SetTag("maintenance.id", command.MaintenanceId);
        activity?.SetTag("duration.minutes", updated.DurationMinutes);

        return OperationResult<CompleteMaintenanceRequestResponse>.Success(new CompleteMaintenanceRequestResponse(
            updated.MaintenanceId,
            updated.RideId,
            updated.Status.ToString(),
            updated.DurationMinutes));
    }
}
