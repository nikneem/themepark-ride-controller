using Dapr.Workflow;
using ThemePark.ControlCenter.Workflow.Activities;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow;

/// <summary>
/// Orchestrates a full ride session lifecycle:
/// 1. Parallel pre-flight checks (fan-out)
/// 2. Riding phase with external chaos event handling (pause/resume loops)
/// 3. Post-ride recording and state-store cleanup
/// 4. Compensation on any failure (stop ride, issue refunds, cleanup)
/// </summary>
public sealed class RideWorkflow : Workflow<RideWorkflowInput, RideWorkflowOutput>
{
    public override async Task<RideWorkflowOutput> RunAsync(
        WorkflowContext context,
        RideWorkflowInput input)
    {
        var startedAt = context.CurrentUtcDateTime;
        var rideInput = new RideTransitionInput(input.RideId, RideStatus.PreFlight);

        // ── 1. Pre-flight fan-out ─────────────────────────────────────────────
        var weatherCheck = context.CallActivityAsync<PreFlightCheckResult>(nameof(CheckWeatherActivity), input.RideId);
        var mascotCheck = context.CallActivityAsync<PreFlightCheckResult>(nameof(CheckMascotZoneActivity), input.RideId);
        var maintenanceCheck = context.CallActivityAsync<PreFlightCheckResult>(nameof(CheckMaintenanceStatusActivity), input.RideId);
        var safetyCheck = context.CallActivityAsync<PreFlightCheckResult>(nameof(CheckSafetySystemsActivity), input.RideId);

        var checks = await Task.WhenAll(weatherCheck, mascotCheck, maintenanceCheck, safetyCheck);

        if (checks.Any(c => !c.IsHealthy))
        {
            return await CompensateAsync(context, input, rideInput,
                "AbortedDueToPreFlightFailure", startedAt);
        }

        // ── 2. State transitions: PreFlight → Loading → Running ───────────────
        await context.CallActivityAsync<RideTransitionOutput>(nameof(StartPreFlightActivity), rideInput);
        await context.CallActivityAsync<RideTransitionOutput>(nameof(StartLoadingActivity), rideInput with { TargetStatus = RideStatus.Loading });
        await context.CallActivityAsync<RideTransitionOutput>(nameof(StartRideActivity), rideInput with { TargetStatus = RideStatus.Running });

        // ── 3. Riding loop — handle external chaos events ─────────────────────
        string outcome;
        while (true)
        {
            var rideEndedTask = context.WaitForExternalEventAsync<string>("ride-session-ended");
            var weatherTask = context.WaitForExternalEventAsync<string>("WeatherAlertReceived");
            var mascotTask = context.WaitForExternalEventAsync<string>("MascotIntrusionReceived");
            var malfunctionTask = context.WaitForExternalEventAsync<string>("MalfunctionReceived");
            var timeoutTask = context.CreateTimer(TimeSpan.FromHours(2));

            var winner = await Task.WhenAny(rideEndedTask, weatherTask, mascotTask, malfunctionTask, timeoutTask);

            if (winner == rideEndedTask)
            {
                var result = await rideEndedTask;
                if (result == "completed")
                {
                    outcome = "Completed";
                    break;
                }
                return await CompensateAsync(context, input, rideInput, "AbortedDueToRideFailure", startedAt);
            }

            if (winner == weatherTask)
            {
                await context.CallActivityAsync<RideTransitionOutput>(nameof(PauseRideActivity), rideInput with { TargetStatus = RideStatus.Paused });

                var clearedTask = context.WaitForExternalEventAsync<string>("WeatherCleared");
                var weatherTimeout = context.CreateTimer(TimeSpan.FromMinutes(10));
                if (await Task.WhenAny(clearedTask, weatherTimeout) == weatherTimeout)
                    return await CompensateAsync(context, input, rideInput, "AbortedDueToWeather", startedAt);

                await context.CallActivityAsync<RideTransitionOutput>(nameof(StartResumingActivity), rideInput with { TargetStatus = RideStatus.Resuming });
                await context.CallActivityAsync<RideTransitionOutput>(nameof(ResumeRideActivity), rideInput with { TargetStatus = RideStatus.Running });
                continue;
            }

            if (winner == mascotTask)
            {
                await context.CallActivityAsync<RideTransitionOutput>(nameof(PauseRideActivity), rideInput with { TargetStatus = RideStatus.Paused });

                var clearedTask = context.WaitForExternalEventAsync<string>("MascotCleared");
                var mascotTimeout = context.CreateTimer(TimeSpan.FromMinutes(5));
                if (await Task.WhenAny(clearedTask, mascotTimeout) == mascotTimeout)
                    return await CompensateAsync(context, input, rideInput, "AbortedDueTOMascot", startedAt);

                await context.CallActivityAsync<RideTransitionOutput>(nameof(StartResumingActivity), rideInput with { TargetStatus = RideStatus.Resuming });
                await context.CallActivityAsync<RideTransitionOutput>(nameof(ResumeRideActivity), rideInput with { TargetStatus = RideStatus.Running });
                continue;
            }

            if (winner == malfunctionTask)
            {
                await context.CallActivityAsync<RideTransitionOutput>(nameof(PauseRideActivity), rideInput with { TargetStatus = RideStatus.Paused });
                await context.CallActivityAsync<RideTransitionOutput>(nameof(EnterMaintenanceActivity), rideInput with { TargetStatus = RideStatus.Maintenance });

                var approvedTask = context.WaitForExternalEventAsync<string>("MaintenanceApproved");
                var maintenanceTimeout = context.CreateTimer(TimeSpan.FromMinutes(30));
                if (await Task.WhenAny(approvedTask, maintenanceTimeout) == maintenanceTimeout)
                    return await CompensateAsync(context, input, rideInput, "AbortedDueToMaintenance", startedAt);

                // Wait for maintenance completion (no timeout — operator drives this).
                await context.WaitForExternalEventAsync<string>("MaintenanceCompleted");

                await context.CallActivityAsync<RideTransitionOutput>(nameof(StartResumingActivity), rideInput with { TargetStatus = RideStatus.Resuming });
                await context.CallActivityAsync<RideTransitionOutput>(nameof(ResumeRideActivity), rideInput with { TargetStatus = RideStatus.Running });
                continue;
            }

            // 2-hour overall timeout.
            return await CompensateAsync(context, input, rideInput, "AbortedDueToTimeout", startedAt);
        }

        // ── 4. Post-ride: complete transition + record session ─────────────────
        await context.CallActivityAsync<RideTransitionOutput>(nameof(CompleteRideActivity), rideInput with { TargetStatus = RideStatus.Completed });

        var sessionId = context.NewGuid();
        await context.CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity),
            new RecordSessionSummaryInput(sessionId, Guid.Parse(input.RideId), startedAt, context.CurrentUtcDateTime, outcome));

        await context.CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), input.RideId);

        return new RideWorkflowOutput(input.RideId, RideStatus.Completed, outcome);
    }

    private static async Task<RideWorkflowOutput> CompensateAsync(
        WorkflowContext context,
        RideWorkflowInput input,
        RideTransitionInput rideInput,
        string outcome,
        DateTimeOffset startedAt)
    {
        await context.CallActivityAsync<RideTransitionOutput>(
            nameof(FailRideActivity), rideInput with { TargetStatus = RideStatus.Failed });

        Guid? refundBatchId = null;
        if (input.Passengers.Count > 0)
        {
            var refundInput = new IssueRefundActivityInput(
                input.RideId, context.InstanceId, outcome, input.Passengers);
            var refundOutput = await context.CallActivityAsync<IssueRefundActivityOutput>(
                nameof(IssueRefundActivity), refundInput);
            refundBatchId = refundOutput.RefundBatchId;
        }

        await context.CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), input.RideId);

        var sessionId = context.NewGuid();
        await context.CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity),
            new RecordSessionSummaryInput(sessionId, Guid.Parse(input.RideId), startedAt, context.CurrentUtcDateTime, outcome));

        return new RideWorkflowOutput(input.RideId, RideStatus.Failed, outcome, refundBatchId);
    }
}
