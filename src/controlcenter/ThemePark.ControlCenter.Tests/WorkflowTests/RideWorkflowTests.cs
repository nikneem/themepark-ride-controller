using Dapr.Workflow;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
    private static readonly string WorkflowId = "wf-test-001";

    private static RideWorkflowInput MakeInput() =>
        new(RideId, WorkflowId, DateTimeOffset.UtcNow, RideDurationSeconds: 1);

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

    /// <summary>All bool-returning activities return true (covers pre-flight + ride lifecycle).</summary>
    private static void SetupBoolActivities(WorkflowContext ctx)
    {
        ctx.CallActivityAsync<bool>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(true));
    }

    private static void SetupLoadPassengers(WorkflowContext ctx, IReadOnlyList<RidePassenger>? passengers = null)
    {
        var result = new LoadPassengersResult(passengers ?? [], 0, false);
        ctx.CallActivityAsync<LoadPassengersResult>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromResult(result));
    }

    /// <summary>Timer fires immediately so the riding loop completes in one iteration.</summary>
    private static void SetupTimerFiresImmediately(WorkflowContext ctx)
    {
        ctx.CreateTimer(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        ctx.WaitForExternalEventAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new TaskCompletionSource<string>().Task);
    }

    // ── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RideWorkflow_HappyPath_ReturnsCompleted()
    {
        var ctx = BuildContext();
        SetupBoolActivities(ctx);
        SetupLoadPassengers(ctx);
        SetupTimerFiresImmediately(ctx);

        var workflow = new RideWorkflow();
        var result = await workflow.RunAsync(ctx, MakeInput());

        Assert.Equal(RideStatus.Completed, result.FinalStatus);
        Assert.Equal("Completed", result.Outcome);
        Assert.Equal(RideId, result.RideId);
        Assert.Null(result.RefundBatchId);
    }

    [Fact]
    public async Task RideWorkflow_HappyPath_CallsCleanupAndRecordSession()
    {
        var ctx = BuildContext();
        SetupBoolActivities(ctx);
        SetupLoadPassengers(ctx);
        SetupTimerFiresImmediately(ctx);

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, MakeInput());

        await ctx.Received(1).CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }

    [Fact]
    public async Task RideWorkflow_AllPreFlightActivitiesCalled()
    {
        var ctx = BuildContext();
        SetupBoolActivities(ctx);
        SetupLoadPassengers(ctx);
        SetupTimerFiresImmediately(ctx);

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, MakeInput());

        await ctx.Received(1).CallActivityAsync<bool>(nameof(CheckRideStatusActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(CheckWeatherActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(CheckMascotActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<LoadPassengersResult>(nameof(LoadPassengersActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }

    // ── Pre-flight failure and compensation ──────────────────────────────────

    [Fact]
    public async Task RideWorkflow_PreFlightFailure_ReturnsFailed()
    {
        var ctx = BuildContext();

        // All bool activities return true (covers compensation path)
        SetupBoolActivities(ctx);

        // But CheckRideStatusActivity throws to trigger pre-flight failure
        ctx.CallActivityAsync<bool>(nameof(CheckRideStatusActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .ThrowsAsync(new InvalidOperationException("Ride is not Idle."));

        var workflow = new RideWorkflow();
        var result = await workflow.RunAsync(ctx, MakeInput());

        Assert.Equal(RideStatus.Failed, result.FinalStatus);
        // Passengers not loaded yet at pre-flight failure time, so no refund batch.
        Assert.Null(result.RefundBatchId);
    }

    [Fact]
    public async Task RideWorkflow_PreFlightFailure_RunsCompensationActivities()
    {
        var ctx = BuildContext();
        SetupBoolActivities(ctx);
        ctx.CallActivityAsync<bool>(nameof(CheckRideStatusActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .ThrowsAsync(new InvalidOperationException("Pre-flight failed."));

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, MakeInput());

        // Compensation path always calls cleanup and record session.
        await ctx.Received(1).CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
        await ctx.Received(1).CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }

    [Fact]
    public async Task RideWorkflow_PreFlightFailure_NoPassengersLoaded_SkipsRefund()
    {
        var ctx = BuildContext();
        SetupBoolActivities(ctx);

        // Weather check throws before LoadPassengersActivity — passengers are never loaded.
        ctx.CallActivityAsync<bool>(nameof(CheckWeatherActivity), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .ThrowsAsync(new InvalidOperationException("Weather unsafe."));

        var workflow = new RideWorkflow();
        await workflow.RunAsync(ctx, MakeInput());

        await ctx.DidNotReceive().CallActivityAsync<IssueRefundActivityOutput>(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>());
    }
}


