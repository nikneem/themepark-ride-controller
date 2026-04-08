using Dapr.Workflow;
using NSubstitute;
using ThemePark.ControlCenter.Workflow;
using ThemePark.ControlCenter.Workflow.Activities;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Tests.WorkflowTests;

/// <summary>
/// Unit tests for <see cref="RideWorkflow"/> using a mocked <see cref="WorkflowContext"/>.
/// Activities are not executed; the context mock controls all outcomes.
/// </summary>
public sealed class RideWorkflowTests
{
    private static readonly string RideId = "a1b2c3d4-0001-0000-0000-000000000001";

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static WorkflowContext BuildContext()
    {
        var ctx = Substitute.For<WorkflowContext>();
        ctx.InstanceId.Returns("test-instance");
        ctx.CurrentUtcDateTime.Returns(DateTime.UtcNow);
        ctx.NewGuid().Returns(Guid.NewGuid());
        ctx.IsReplaying.Returns(false);
        return ctx;
    }

    private static void SetupAllPreFlightHealthy(WorkflowContext ctx)
    {
        var ok = new PreFlightCheckResult(true, "OK");
        ctx.CallActivityAsync<PreFlightCheckResult>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(ok));
    }

    private static void SetupTransitionActivities(WorkflowContext ctx)
    {
        var output = new RideTransitionOutput(RideId, RideStatus.Running);
        ctx.CallActivityAsync<RideTransitionOutput>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(output));
    }

    private static void SetupBoolActivities(WorkflowContext ctx)
    {
        ctx.CallActivityAsync<bool>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(true));
    }

    private static void SetupNeverFireTimer(WorkflowContext ctx)
    {
        ctx.CreateTimer(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => new TaskCompletionSource().Task);
    }

    private static void SetupExternalEvents(WorkflowContext ctx, string rideEndedPayload = "completed")
    {
        // ride-session-ended fires with the given payload.
        ctx.WaitForExternalEventAsync<string>("ride-session-ended", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(rideEndedPayload));

        // All other events never fire — Task.WhenAny will pick ride-session-ended.
        ctx.WaitForExternalEventAsync<string>(
                Arg.Is<string>(s => s != "ride-session-ended"), Arg.Any<CancellationToken>())
            .Returns(_ => new TaskCompletionSource<string>().Task);
    }

    // ── Task 9.3: Happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task RideWorkflow_HappyPath_ReturnsCompleted()
    {
        var ctx = BuildContext();
        SetupAllPreFlightHealthy(ctx);
        SetupTransitionActivities(ctx);
        SetupBoolActivities(ctx);
        SetupNeverFireTimer(ctx);
        SetupExternalEvents(ctx, "completed");

        var workflow = new RideWorkflow();
        var result = await workflow.RunAsync(ctx, new RideWorkflowInput(RideId, []));

        Assert.Equal(RideStatus.Completed, result.FinalStatus);
        Assert.Equal("Completed", result.Outcome);
        Assert.Equal(RideId, result.RideId);
        Assert.Null(result.RefundBatchId);
    }

    [Fact]
    public async Task RideWorkflow_HappyPath_CallsCleanupAndRecordSession()
    {
        var ctx = BuildContext();
        SetupAllPreFlightHealthy(ctx);
        SetupTransitionActivities(ctx);
        SetupBoolActivities(ctx);
        SetupNeverFireTimer(ctx);
        SetupExternalEvents(ctx, "completed");

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, new RideWorkflowInput(RideId, []));

        await ctx.Received(1).CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }

    [Fact]
    public async Task RideWorkflow_AllFourPreFlightActivitiesCalled()
    {
        var ctx = BuildContext();
        SetupAllPreFlightHealthy(ctx);
        SetupTransitionActivities(ctx);
        SetupBoolActivities(ctx);
        SetupNeverFireTimer(ctx);
        SetupExternalEvents(ctx, "completed");

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, new RideWorkflowInput(RideId, []));

        await ctx.Received(1).CallActivityAsync<PreFlightCheckResult>(nameof(CheckWeatherActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<PreFlightCheckResult>(nameof(CheckMascotZoneActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<PreFlightCheckResult>(nameof(CheckMaintenanceStatusActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<PreFlightCheckResult>(nameof(CheckSafetySystemsActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }

    // ── Task 9.4: Pre-flight failure and compensation ─────────────────────────

    [Fact]
    public async Task RideWorkflow_PreFlightFailure_ReturnsFailed()
    {
        var ctx = BuildContext();

        // One check fails.
        var fail = new PreFlightCheckResult(false, "Weather unsafe");
        var ok = new PreFlightCheckResult(true, "OK");
        ctx.CallActivityAsync<PreFlightCheckResult>(nameof(CheckWeatherActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(fail));
        ctx.CallActivityAsync<PreFlightCheckResult>(
                Arg.Is<string>(s => s != nameof(CheckWeatherActivity)), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(ok));

        SetupTransitionActivities(ctx);
        SetupBoolActivities(ctx);

        // Refund activity needed for compensation path with passengers.
        var refundBatchId = Guid.NewGuid();
        ctx.CallActivityAsync<IssueRefundActivityOutput>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(new IssueRefundActivityOutput(refundBatchId, 2, 50.00m, 0)));

        var passengers = new List<RidePassenger>
        {
            new("passenger-1", false),
            new("passenger-2", true)
        };

        var workflow = new RideWorkflow();
        var result = await workflow.RunAsync(ctx, new RideWorkflowInput(RideId, passengers));

        Assert.Equal(RideStatus.Failed, result.FinalStatus);
        Assert.Equal("AbortedDueToPreFlightFailure", result.Outcome);
        Assert.Equal(refundBatchId, result.RefundBatchId);
    }

    [Fact]
    public async Task RideWorkflow_PreFlightFailure_RunsCompensationActivities()
    {
        var ctx = BuildContext();

        var fail = new PreFlightCheckResult(false, "Mascot in zone");
        ctx.CallActivityAsync<PreFlightCheckResult>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(fail));

        SetupTransitionActivities(ctx);
        SetupBoolActivities(ctx);

        ctx.CallActivityAsync<IssueRefundActivityOutput>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(new IssueRefundActivityOutput(Guid.NewGuid(), 1, 25.00m, 0)));

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, new RideWorkflowInput(RideId, [new("p1", false)]));

        await ctx.Received(1).CallActivityAsync<RideTransitionOutput>(nameof(FailRideActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<IssueRefundActivityOutput>(nameof(IssueRefundActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }

    [Fact]
    public async Task RideWorkflow_PreFlightFailure_NoPassengers_SkipsRefund()
    {
        var ctx = BuildContext();

        var fail = new PreFlightCheckResult(false, "Systems offline");
        ctx.CallActivityAsync<PreFlightCheckResult>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(fail));

        SetupTransitionActivities(ctx);
        SetupBoolActivities(ctx);

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, new RideWorkflowInput(RideId, []));

        await ctx.DidNotReceive().CallActivityAsync<IssueRefundActivityOutput>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }
}


