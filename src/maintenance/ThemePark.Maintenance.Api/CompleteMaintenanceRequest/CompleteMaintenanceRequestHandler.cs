using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using ThemePark.EventContracts.Events;
using ThemePark.Maintenance.Api.State;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Api.CompleteMaintenanceRequest;

public sealed record CompleteMaintenanceRequestCommand(Guid MaintenanceId);

public sealed record CompleteMaintenanceRequestResponse(
    Guid MaintenanceId,
    Guid RideId,
    string Status,
    int? DurationMinutes);

public sealed class CompleteMaintenanceRequestHandler(
    IMaintenanceStateStore stateStore,
    DaprClient daprClient)
{
    private static readonly ActivitySource ActivitySource = new("ThemePark.Maintenance.Api");

    public async Task<Results<Ok<CompleteMaintenanceRequestResponse>, NotFound, Conflict<string>>> HandleAsync(
        CompleteMaintenanceRequestCommand command,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("CompleteMaintenanceRequest");

        var record = await stateStore.GetRecordAsync(command.MaintenanceId, ct);
        if (record is null)
            return TypedResults.NotFound();

        if (record.Status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled)
            return TypedResults.Conflict(
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

        return TypedResults.Ok(new CompleteMaintenanceRequestResponse(
            updated.MaintenanceId,
            updated.RideId,
            updated.Status.ToString(),
            updated.DurationMinutes));
    }
}
