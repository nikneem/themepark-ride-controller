using Dapr.Workflow;
using ThemePark.ControlCenter.Workflow.Activities;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow;

/// <summary>
/// Orchestrates the full ride session lifecycle:
/// 1. Pre-flight: status check → parallel weather + mascot → load passengers → start ride
/// 2. Riding loop: duration timer OR chaos events (weather/mascot/malfunction)
/// 3. Post-ride: complete → record session → cleanup
/// 4. Compensation on any failure: stop ride → refund passengers → cleanup
/// </summary>
public sealed class RideWorkflow : Workflow<RideWorkflowInput, RideWorkflowOutput>
{
    private static readonly WorkflowTaskOptions ActivityOptions = new()
    {
        RetryPolicy = new WorkflowRetryPolicy(
            maxNumberOfAttempts: 3,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromSeconds(8),
            retryTimeout: TimeSpan.FromSeconds(30))
    };

    public override async Task<RideWorkflowOutput> RunAsync(
        WorkflowContext context,
        RideWorkflowInput input)
    {
        var startedAt = input.StartedAt;
        var rideId = input.RideId;
        var rideStarted = false;
        IReadOnlyList<RidePassenger> passengers = [];

        try
        {
            // ── 1. Pre-flight sequence ────────────────────────────────────────────
            await context.CallActivityAsync<bool>(
                nameof(CheckRideStatusActivity), rideId, ActivityOptions);

            await Task.WhenAll(
                context.CallActivityAsync<bool>(nameof(CheckWeatherActivity), rideId, ActivityOptions),
                context.CallActivityAsync<bool>(nameof(CheckMascotActivity), rideId, ActivityOptions));

            var loadResult = await context.CallActivityAsync<LoadPassengersResult>(
                nameof(LoadPassengersActivity), rideId, ActivityOptions);
            passengers = loadResult.Passengers;

            await context.CallActivityAsync<bool>(nameof(StartRideActivity), rideId, ActivityOptions);
            rideStarted = true;

            // ── 2. Riding loop ────────────────────────────────────────────────────
            var rideDuration = TimeSpan.FromSeconds(input.RideDurationSeconds);
            string outcome;

            while (true)
            {
                var timerTask = context.CreateTimer(rideDuration);
                var weatherTask = context.WaitForExternalEventAsync<string>("WeatherAlertReceived");
                var mascotTask = context.WaitForExternalEventAsync<string>("MascotIntrusionReceived");
                var malfunctionTask = context.WaitForExternalEventAsync<string>("MalfunctionReceived");

                var winner = await Task.WhenAny(timerTask, weatherTask, mascotTask, malfunctionTask);

                if (winner == timerTask)
                {
                    outcome = "Completed";
                    break;
                }

                if (winner == weatherTask)
                {
                    var severity = await weatherTask;

                    if (string.Equals(severity, WeatherSeverity.Severe.ToString(), StringComparison.OrdinalIgnoreCase))
                        return await CompensateAsync(context, input, rideStarted, passengers,
                            "AbortedDueToSevereWeather", startedAt);

                    // Mild weather — pause and wait up to 10 min for resolution.
                    await context.CallActivityAsync<bool>(nameof(PauseRideActivity),
                        new PauseRideActivityInput(rideId, $"Weather alert: {severity}"), ActivityOptions);

                    var resolvedTask = context.WaitForExternalEventAsync<string>("ChaosEventResolved");
                    var weatherTimeout = context.CreateTimer(TimeSpan.FromMinutes(10));
                    if (await Task.WhenAny(resolvedTask, weatherTimeout) == weatherTimeout)
                        return await CompensateAsync(context, input, rideStarted, passengers,
                            "AbortedDueToWeatherTimeout", startedAt);

                    await context.CallActivityAsync<bool>(nameof(ResumeRideActivity), rideId, ActivityOptions);
                    continue;
                }

                if (winner == mascotTask)
                {
                    await context.CallActivityAsync<bool>(nameof(PauseRideActivity),
                        new PauseRideActivityInput(rideId, "Mascot intrusion detected"), ActivityOptions);

                    var resolvedTask = context.WaitForExternalEventAsync<string>("ChaosEventResolved");
                    var mascotTimeout = context.CreateTimer(TimeSpan.FromMinutes(5));
                    // Auto-resume on timeout — mascot clears on its own.
                    await Task.WhenAny(resolvedTask, mascotTimeout);

                    await context.CallActivityAsync<bool>(nameof(ResumeRideActivity), rideId, ActivityOptions);
                    continue;
                }

                if (winner == malfunctionTask)
                {
                    await context.CallActivityAsync<bool>(nameof(PauseRideActivity),
                        new PauseRideActivityInput(rideId, "Malfunction reported"), ActivityOptions);

                    await context.CallActivityAsync<bool>(nameof(TriggerMaintenanceActivity),
                        new TriggerMaintenanceActivityInput(
                            Guid.Parse(rideId), input.WorkflowId, "Malfunction during ride"),
                        ActivityOptions);

                    var approvedTask = context.WaitForExternalEventAsync<string>("MaintenanceApproved");
                    var maintenanceTimeout = context.CreateTimer(TimeSpan.FromMinutes(30));
                    if (await Task.WhenAny(approvedTask, maintenanceTimeout) == maintenanceTimeout)
                        return await CompensateAsync(context, input, rideStarted, passengers,
                            "AbortedDueToMaintenanceTimeout", startedAt);

                    // Operator signals completion via ChaosEventResolved.
                    await context.WaitForExternalEventAsync<string>("ChaosEventResolved");

                    await context.CallActivityAsync<bool>(nameof(ResumeRideActivity), rideId, ActivityOptions);
                    continue;
                }
            }

            // ── 3. Post-ride ──────────────────────────────────────────────────────
            await context.CallActivityAsync<bool>(nameof(CompleteRideActivity), rideId, ActivityOptions);

            var sessionId = context.NewGuid();
            await context.CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity),
                new RecordSessionSummaryInput(
                    sessionId, Guid.Parse(rideId), startedAt, context.CurrentUtcDateTime, outcome));

            await context.CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), rideId);

            return new RideWorkflowOutput(rideId, RideStatus.Completed, outcome);
        }
        catch (Exception ex)
        {
            return await CompensateAsync(context, input, rideStarted, passengers,
                $"AbortedDueToError: {ex.Message}", startedAt);
        }
    }

    private static async Task<RideWorkflowOutput> CompensateAsync(
        WorkflowContext context,
        RideWorkflowInput input,
        bool rideStarted,
        IReadOnlyList<RidePassenger> passengers,
        string outcome,
        DateTimeOffset startedAt)
    {
        if (rideStarted)
        {
            try
            {
                await context.CallActivityAsync<bool>(nameof(CompleteRideActivity), input.RideId);
            }
            catch { /* best effort */ }
        }

        Guid? refundBatchId = null;
        if (passengers.Count > 0)
        {
            var refundInput = new IssueRefundActivityInput(
                input.RideId, input.WorkflowId, outcome, passengers);
            var refundOutput = await context.CallActivityAsync<IssueRefundActivityOutput>(
                nameof(IssueRefundActivity), refundInput);
            refundBatchId = refundOutput.RefundBatchId;
        }

        await context.CallActivityAsync<bool>(nameof(CleanupWorkflowActivity), input.RideId);

        var sessionId = context.NewGuid();
        await context.CallActivityAsync<bool>(nameof(RecordSessionSummaryActivity),
            new RecordSessionSummaryInput(
                sessionId, Guid.Parse(input.RideId), startedAt, context.CurrentUtcDateTime, outcome));

        return new RideWorkflowOutput(input.RideId, RideStatus.Failed, outcome, refundBatchId);
    }
}
