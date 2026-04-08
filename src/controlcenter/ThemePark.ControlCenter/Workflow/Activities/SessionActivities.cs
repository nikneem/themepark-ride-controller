using Dapr.Client;
using Dapr.Workflow;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.ControlCenter.Domain;
using ThemePark.Rides.Infrastructure;
using ThemePark.ControlCenter.PubSub;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow.Activities;

/// <summary>Input for <see cref="RecordSessionSummaryActivity"/>.</summary>
public sealed record RecordSessionSummaryInput(
    Guid SessionId,
    Guid RideId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string Outcome);

/// <summary>
/// Transitions the ride to Running — used as the activity that "starts" the ride
/// after all pre-flight checks have passed.
/// </summary>
public sealed class StartRideActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Running, repository, publisher, nameof(StartRideActivity));
}

/// <summary>
/// Transitions the ride to Failed — used in the compensation path.
/// </summary>
public sealed class StopRideActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Failed, repository, publisher, nameof(StopRideActivity));
}

/// <summary>
/// Removes the <c>active-workflow-{rideId}</c> state store entry when the workflow terminates,
/// preventing subsequent pub/sub events from raising external events into a finished workflow.
/// </summary>
public sealed class CleanupWorkflowActivity(DaprClient daprClient)
    : WorkflowActivity<string, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        await daprClient.DeleteStateAsync(AspireConstants.DaprComponents.StateStore, $"active-workflow-{rideId}");
        return true;
    }
}

/// <summary>
/// Persists a completed session summary to the Dapr state store and appends
/// the session ID to the per-ride session index so history queries can find it.
/// </summary>
public sealed class RecordSessionSummaryActivity(DaprClient daprClient)
    : WorkflowActivity<RecordSessionSummaryInput, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, RecordSessionSummaryInput input)
    {
        var session = new RideSession(
            input.SessionId,
            input.RideId,
            input.StartedAt,
            input.CompletedAt,
            input.Outcome);

        // Persist the session summary.
        await daprClient.SaveStateAsync(AspireConstants.DaprComponents.StateStore, $"session-summary-{input.SessionId}", session);

        // Append session ID to the ride's session index (last 20 kept).
        var index = await daprClient.GetStateAsync<List<Guid>?>(AspireConstants.DaprComponents.StateStore, $"sessions-{input.RideId}") ?? [];
        index.Add(input.SessionId);
        if (index.Count > 20)
            index = index[^20..];

        await daprClient.SaveStateAsync(AspireConstants.DaprComponents.StateStore, $"sessions-{input.RideId}", index);
        return true;
    }
}
