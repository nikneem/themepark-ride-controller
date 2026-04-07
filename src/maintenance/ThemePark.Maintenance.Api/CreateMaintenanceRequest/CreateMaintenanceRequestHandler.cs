using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using ThemePark.EventContracts.Events;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Api.CreateMaintenanceRequest;

public sealed record CreateMaintenanceRequestCommand(
    Guid RideId,
    string Reason,
    string? WorkflowId,
    DateTimeOffset RequestedAt);

public sealed record CreateMaintenanceRequestResponse(
    Guid MaintenanceId,
    Guid RideId,
    string Status);

public sealed class CreateMaintenanceRequestHandler(
    IMaintenanceStateStore stateStore,
    DaprClient daprClient)
{
    private static readonly ActivitySource ActivitySource = new("ThemePark.Maintenance.Api");

    public async Task<Results<Created<CreateMaintenanceRequestResponse>, BadRequest<string>>> HandleAsync(
        CreateMaintenanceRequestCommand command,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("CreateMaintenanceRequest");

        if (command.RideId == Guid.Empty)
            return TypedResults.BadRequest("rideId is required.");

        if (!Enum.TryParse<MaintenanceReason>(command.Reason, ignoreCase: true, out var reason))
            return TypedResults.BadRequest(
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

        return TypedResults.Created(
            $"/maintenance/{maintenanceId}",
            new CreateMaintenanceRequestResponse(maintenanceId, command.RideId, MaintenanceStatus.Pending.ToString()));
    }
}
