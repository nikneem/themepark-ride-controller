using Dapr.Client;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace ThemePark.ControlCenter.Features.ApproveMaintenance;

/// <summary>
/// Approves a pending maintenance request by raising the "MaintenanceApproved" workflow event
/// on the active ride workflow instance.
/// </summary>
public sealed class ApproveMaintenanceHandler(
    DaprClient daprClient,
    DaprWorkflowClient workflowClient,
    ILogger<ApproveMaintenanceHandler> logger)
{
    private const string StoreName = "themepark-statestore";

    /// <summary>
    /// Returns <c>true</c> when the event was raised successfully;
    /// <c>false</c> when no active workflow exists for the ride (caller should return 404).
    /// </summary>
    public async Task<bool> HandleAsync(
        ApproveMaintenanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var instanceId = await daprClient.GetStateAsync<string?>(
            StoreName,
            $"active-workflow-{command.RideId}",
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(instanceId))
        {
            logger.LogWarning("No active workflow found for ride {RideId}. Cannot approve maintenance.", command.RideId);
            return false;
        }

        await workflowClient.RaiseEventAsync(instanceId, "MaintenanceApproved", true, cancellationToken);

        logger.LogInformation("Raised MaintenanceApproved event on workflow {InstanceId} for ride {RideId}.", instanceId, command.RideId);
        return true;
    }
}
