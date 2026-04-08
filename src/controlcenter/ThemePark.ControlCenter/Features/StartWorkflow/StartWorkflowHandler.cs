using Dapr.Workflow;
using ThemePark.ControlCenter.Workflow;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Enums;
using ThemePark.Shared.Workflows;

namespace ThemePark.ControlCenter.Features.StartWorkflow;

/// <summary>
/// Handles the <see cref="StartWorkflowCommand"/> by enforcing domain invariants
/// and scheduling a new <see cref="RideWorkflow"/> instance via Dapr.
/// </summary>
public sealed class StartWorkflowHandler(
    IRideStateRepository rideStateRepository,
    DaprWorkflowClient workflowClient)
{
    public async Task<OperationResult<StartWorkflowResponse>> HandleAsync(
        StartWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(command.RideId, out var rideGuid))
            return OperationResult<StartWorkflowResponse>.BadRequest("Invalid rideId format.");

        var conflict = await CheckForActiveWorkflowAsync(command.RideId, cancellationToken);
        if (conflict is not null)
            return conflict;

        var workflowId = WorkflowIdFactory.Create(rideGuid, DateTime.UtcNow);

        var input = new RideWorkflowInput(
            command.RideId,
            command.Passengers,
            command.RefundReason);

        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(RideWorkflow),
            instanceId: workflowId,
            input: input);

        return OperationResult<StartWorkflowResponse>.Success(new StartWorkflowResponse(workflowId));
    }

    /// <summary>
    /// Guards the at-most-one-active-workflow-per-ride invariant defined in
    /// <c>core-domain-concepts</c>: at any point in time no ride may have more than one
    /// active Dapr Workflow instance. Returns an HTTP 409 result when the ride's current
    /// status indicates an active workflow is already running; returns <c>null</c> when
    /// it is safe to proceed.
    /// </summary>
    private async Task<OperationResult<StartWorkflowResponse>?> CheckForActiveWorkflowAsync(
        string rideId,
        CancellationToken cancellationToken)
    {
        var status = await rideStateRepository.GetStatusAsync(rideId, cancellationToken);

        if (status != RideStatus.Idle)
            return OperationResult<StartWorkflowResponse>.Conflict(
                $"Ride '{rideId}' already has an active workflow (current status: {status}). " +
                "A new session can only be started from Idle status.");

        return null;
    }
}

/// <summary>Response returned when a workflow is successfully scheduled.</summary>
public sealed record StartWorkflowResponse(string WorkflowId);
