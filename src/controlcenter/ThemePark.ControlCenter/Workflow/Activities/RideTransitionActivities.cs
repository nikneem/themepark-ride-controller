using Dapr.Client;
using Dapr.Workflow;
using System.Net.Http.Json;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.ControlCenter.PubSub;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.StateMachine;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow.Activities;

// Local DTOs for rides-service requests.
internal sealed record PauseRideRequest(string? Reason);

/// <summary>
/// Pauses the ride by calling <c>POST /rides/{rideId}/pause</c> on rides-service via Dapr.
/// </summary>
public sealed class PauseRideActivity : WorkflowActivity<PauseRideActivityInput, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.RidesApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, PauseRideActivityInput input)
    {
        var request = new PauseRideRequest(input.Reason);
        var httpResponse = await HttpClient.PostAsJsonAsync($"/rides/{input.RideId}/pause", request);
        httpResponse.EnsureSuccessStatusCode();
        return true;
    }
}

/// <summary>
/// Resumes the ride by calling <c>POST /rides/{rideId}/resume</c> on rides-service via Dapr.
/// </summary>
public sealed class ResumeRideActivity : WorkflowActivity<string, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.RidesApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        var httpResponse = await HttpClient.PostAsync($"/rides/{rideId}/resume", null);
        httpResponse.EnsureSuccessStatusCode();
        return true;
    }
}

/// <summary>
/// Stops/completes the ride by calling <c>POST /rides/{rideId}/stop</c> on rides-service via Dapr.
/// Used for both successful completion and failure termination.
/// </summary>
public sealed class CompleteRideActivity : WorkflowActivity<string, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.RidesApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        var httpResponse = await HttpClient.PostAsync($"/rides/{rideId}/stop", null);
        httpResponse.EnsureSuccessStatusCode();
        return true;
    }
}

// Legacy state-machine activities — kept for backward compatibility, not used by updated RideWorkflow.

/// <summary>
/// Shared base helper for legacy ride transition activities.
/// Reads state, applies the machine transition, persists the result, and publishes the status-changed event.
/// </summary>
internal static class RideTransitionHelper
{
    internal static async Task<RideTransitionOutput> ExecuteAsync(
        WorkflowActivityContext context,
        RideTransitionInput input,
        RideStatus target,
        IRideStateRepository repository,
        IRideStatusEventPublisher publisher,
        string activityName)
    {
        var current = await repository.GetStatusAsync(input.RideId);
        var machine = new RideStateMachine(input.RideId, current);
        machine.Transition(target);
        await repository.SaveStatusAsync(input.RideId, machine.CurrentStatus);

        await publisher.PublishAsync(
            input.RideId,
            current,
            machine.CurrentStatus,
            activityName);

        machine.ClearEvents();
        return new RideTransitionOutput(input.RideId, machine.CurrentStatus);
    }
}

/// <summary>Transitions a ride from Idle → PreFlight.</summary>
public sealed class StartPreFlightActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.PreFlight, repository, publisher, nameof(StartPreFlightActivity));
}

/// <summary>Transitions a ride from PreFlight → Loading.</summary>
public sealed class StartLoadingActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Loading, repository, publisher, nameof(StartLoadingActivity));
}

/// <summary>Transitions a ride from Loading → Running.</summary>
public sealed class StartRunActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Running, repository, publisher, nameof(StartRunActivity));
}

/// <summary>Transitions a ride from Running → Maintenance.</summary>
public sealed class EnterMaintenanceActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Maintenance, repository, publisher, nameof(EnterMaintenanceActivity));
}

/// <summary>Transitions a ride from Maintenance → Resuming.</summary>
public sealed class StartResumingActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Resuming, repository, publisher, nameof(StartResumingActivity));
}

/// <summary>Transitions a ride to Failed (from any valid state).</summary>
public sealed class FailRideActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Failed, repository, publisher, nameof(FailRideActivity));
}

/// <summary>Resets a ride from Completed|Failed → Idle.</summary>
public sealed class ResetRideActivity(IRideStateRepository repository, IRideStatusEventPublisher publisher)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Idle, repository, publisher, nameof(ResetRideActivity));
}
