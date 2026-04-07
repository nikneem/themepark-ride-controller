using Dapr.Workflow;
using ThemePark.ControlCenter.Workflow.Activities;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow;

/// <summary>
/// Orchestrates a single ride session through its lifecycle.
/// On failure, compensates by issuing refunds to all passengers via IssueRefundActivity.
/// </summary>
public sealed class RideWorkflow : Workflow<RideWorkflowInput, RideWorkflowOutput>
{
    public override async Task<RideWorkflowOutput> RunAsync(
        WorkflowContext context,
        RideWorkflowInput input)
    {
        var rideInput = new RideTransitionInput(input.RideId, RideStatus.PreFlight);

        try
        {
            // PreFlight → Loading → Running (happy path)
            await context.CallActivityAsync<RideTransitionOutput>(
                nameof(StartPreFlightActivity), rideInput);

            await context.CallActivityAsync<RideTransitionOutput>(
                nameof(StartLoadingActivity), rideInput with { TargetStatus = RideStatus.Loading });

            await context.CallActivityAsync<RideTransitionOutput>(
                nameof(StartRunActivity), rideInput with { TargetStatus = RideStatus.Running });

            // Wait for ride completion or failure signal
            var completionEvent = await context.WaitForExternalEventAsync<string>(
                "ride-session-ended",
                timeout: TimeSpan.FromHours(2));

            if (completionEvent == "completed")
            {
                await context.CallActivityAsync<RideTransitionOutput>(
                    nameof(CompleteRideActivity), rideInput with { TargetStatus = RideStatus.Completed });

                return new RideWorkflowOutput(input.RideId, RideStatus.Completed);
            }

            // Failure path — fall through to compensation below
            throw new InvalidOperationException($"Ride {input.RideId} ended with failure: {completionEvent}");
        }
        catch (Exception)
        {
            // Compensation: issue refunds to all passengers
            await context.CallActivityAsync<RideTransitionOutput>(
                nameof(FailRideActivity), rideInput with { TargetStatus = RideStatus.Failed });

            if (input.Passengers.Count > 0)
            {
                var refundInput = new IssueRefundActivityInput(
                    input.RideId,
                    context.InstanceId,
                    input.RefundReason,
                    input.Passengers);

                var refundOutput = await context.CallActivityAsync<IssueRefundActivityOutput>(
                    nameof(IssueRefundActivity), refundInput);

                return new RideWorkflowOutput(input.RideId, RideStatus.Failed, refundOutput.RefundBatchId);
            }

            return new RideWorkflowOutput(input.RideId, RideStatus.Failed);
        }
    }
}
