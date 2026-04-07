using Dapr.Workflow;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.StateMachine;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow.Activities;

/// <summary>
/// Shared base helper for ride transition activities.
/// Reads state, applies the machine transition, and persists the result.
/// </summary>
internal static class RideTransitionHelper
{
    internal static async Task<RideTransitionOutput> ExecuteAsync(
        WorkflowActivityContext context,
        RideTransitionInput input,
        RideStatus target,
        IRideStateRepository repository)
    {
        var current = await repository.GetStatusAsync(input.RideId);
        var machine = new RideStateMachine(input.RideId, current);
        machine.Transition(target);
        await repository.SaveStatusAsync(input.RideId, machine.CurrentStatus);
        machine.ClearEvents();
        return new RideTransitionOutput(input.RideId, machine.CurrentStatus);
    }
}

/// <summary>Transitions a ride from Idle → PreFlight.</summary>
public sealed class StartPreFlightActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.PreFlight, repository);
}

/// <summary>Transitions a ride from PreFlight → Loading.</summary>
public sealed class StartLoadingActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Loading, repository);
}

/// <summary>Transitions a ride from Loading → Running.</summary>
public sealed class StartRunActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Running, repository);
}

/// <summary>Transitions a ride from Running → Paused.</summary>
public sealed class PauseRideActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Paused, repository);
}

/// <summary>Transitions a ride from Paused|Resuming → Running.</summary>
public sealed class ResumeRideActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Running, repository);
}

/// <summary>Transitions a ride from Running → Maintenance.</summary>
public sealed class EnterMaintenanceActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Maintenance, repository);
}

/// <summary>Transitions a ride from Maintenance → Resuming.</summary>
public sealed class StartResumingActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Resuming, repository);
}

/// <summary>Transitions a ride to Completed (from Running).</summary>
public sealed class CompleteRideActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Completed, repository);
}

/// <summary>Transitions a ride to Failed (from any valid state).</summary>
public sealed class FailRideActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Failed, repository);
}

/// <summary>Resets a ride from Completed|Failed → Idle.</summary>
public sealed class ResetRideActivity(IRideStateRepository repository)
    : WorkflowActivity<RideTransitionInput, RideTransitionOutput>
{
    public override Task<RideTransitionOutput> RunAsync(WorkflowActivityContext context, RideTransitionInput input) =>
        RideTransitionHelper.ExecuteAsync(context, input, RideStatus.Idle, repository);
}
