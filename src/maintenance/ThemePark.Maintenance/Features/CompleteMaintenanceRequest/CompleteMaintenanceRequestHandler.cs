using Dapr.Client;
using System.Diagnostics;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.EventContracts.Events;
using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Features.CompleteMaintenanceRequest;

public sealed class CompleteMaintenanceRequestHandler(
    IMaintenanceStateStore stateStore,
    DaprClient daprClient)
    : ICommandHandler<CompleteMaintenanceRequestCommand, OperationResult<CompleteMaintenanceRequestResponse>>
{
    private static readonly ActivitySource ActivitySource = new("ThemePark.Maintenance");

    public async Task<OperationResult<CompleteMaintenanceRequestResponse>> HandleAsync(
        CompleteMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("CompleteMaintenanceRequest");

        var record = await stateStore.GetRecordAsync(command.MaintenanceId, cancellationToken);
        if (record is null)
            return OperationResult<CompleteMaintenanceRequestResponse>.NotFound();

        if (record.Status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled)
            return OperationResult<CompleteMaintenanceRequestResponse>.Conflict(
                $"Maintenance record {command.MaintenanceId} is already in terminal state '{record.Status}'.");

        var completedAt = DateTimeOffset.UtcNow;
        var updated = record.WithStatus(MaintenanceStatus.Completed, completedAt);
        await stateStore.SaveRecordAsync(updated, cancellationToken);

        var evt = new MaintenanceCompletedEvent(
            Guid.NewGuid(),
            record.MaintenanceId.ToString(),
            record.RideId,
            completedAt);

        await daprClient.PublishEventAsync(AspireConstants.DaprComponents.PubSub, "maintenance.completed", evt, cancellationToken);

        activity?.SetTag("maintenance.id", command.MaintenanceId);
        activity?.SetTag("duration.minutes", updated.DurationMinutes);

        return OperationResult<CompleteMaintenanceRequestResponse>.Success(new CompleteMaintenanceRequestResponse(
            updated.MaintenanceId,
            updated.RideId,
            updated.Status.ToString(),
            updated.DurationMinutes));
    }
}
