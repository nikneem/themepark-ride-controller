using Dapr.Client;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;
using ThemePark.Aspire.ServiceDefaults;

namespace ThemePark.ControlCenter.Features.ResolveChaosEvent;

/// <summary>
/// Resolves an active chaos event by raising the corresponding workflow event on the
/// active ride workflow instance.
/// </summary>
public sealed class ResolveChaosEventHandler(
    DaprClient daprClient,
    DaprWorkflowClient workflowClient,
    ILogger<ResolveChaosEventHandler> logger)
{
    private const string StoreName = AspireConstants.DaprComponents.StateStore;

    /// <summary>
    /// Returns <c>true</c> when the event was raised successfully;
    /// <c>false</c> when no active workflow exists for the ride (caller should return 404).
    /// </summary>
    public async Task<bool> HandleAsync(
        ResolveChaosEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var instanceId = await daprClient.GetStateAsync<string?>(
            StoreName,
            $"active-workflow-{command.RideId}",
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(instanceId))
        {
            logger.LogWarning(
                "No active workflow found for ride {RideId}. Cannot resolve chaos event {EventId} ({EventType}).",
                command.RideId, command.EventId, command.EventType);
            return false;
        }

        var workflowEventName = "ChaosEventResolved";

        await workflowClient.RaiseEventAsync(instanceId, workflowEventName, command.EventId, cancellationToken);

        logger.LogInformation(
            "Raised {WorkflowEvent} event on workflow {InstanceId} for ride {RideId} (chaos event {EventId}, type {EventType}).",
            workflowEventName, instanceId, command.RideId, command.EventId, command.EventType);

        return true;
    }
}
