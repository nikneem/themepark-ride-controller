using Dapr.Client;
using System.Diagnostics;
using ThemePark.EventContracts.Events;
using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Features.CreateMaintenanceRequest;

public sealed class CreateMaintenanceRequestHandler(
    IMaintenanceStateStore stateStore,
    DaprClient daprClient)
{
    private static readonly ActivitySource ActivitySource = new("ThemePark.Maintenance");

    public async Task<OperationResult<CreateMaintenanceRequestResponse>> HandleAsync(
        CreateMaintenanceRequestCommand command,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("CreateMaintenanceRequest");

        if (command.RideId == Guid.Empty)
            return OperationResult<CreateMaintenanceRequestResponse>.BadRequest("rideId is required.");

        if (!Enum.TryParse<MaintenanceReason>(command.Reason, ignoreCase: true, out var reason))
            return OperationResult<CreateMaintenanceRequestResponse>.BadRequest(
                $"Invalid reason '{command.Reason}'. Valid values: MechanicalFailure, ScheduledCheck, Failure.");

        var maintenanceId = Guid.NewGuid();
        var record = new MaintenanceRecord(
            maintenanceId,
            command.RideId,
            reason,
            MaintenanceStatus.Pending,
            command.WorkflowId,
            command.RequestedAt == default ? DateTimeOffset.UtcNow : command.RequestedAt,
            null);

        await stateStore.SaveRecordAsync(record, ct);
        await stateStore.AppendToRideHistoryAsync(command.RideId, maintenanceId, ct);

        var evt = new MaintenanceRequestedEvent(
            Guid.NewGuid(),
            maintenanceId.ToString(),
            command.RideId,
            reason.ToString(),
            record.RequestedAt);

        await daprClient.PublishEventAsync("themepark-pubsub", "maintenance.requested", evt, ct);

        activity?.SetTag("maintenance.id", maintenanceId);
        activity?.SetTag("ride.id", command.RideId);

        return OperationResult<CreateMaintenanceRequestResponse>.Success(
            new CreateMaintenanceRequestResponse(maintenanceId, command.RideId, MaintenanceStatus.Pending.ToString()));
    }
}
