using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow;

/// <summary>Input for workflow activities that trigger a ride state transition.</summary>
public sealed record RideTransitionInput(string RideId, RideStatus TargetStatus);

/// <summary>Output from workflow activities after a successful state transition.</summary>
public sealed record RideTransitionOutput(string RideId, RideStatus NewStatus);

/// <summary>A passenger on a ride, used for refund calculation on failure.</summary>
public sealed record RidePassenger(string PassengerId, bool IsVip);

/// <summary>Input to start a RideWorkflow instance.</summary>
public sealed record RideWorkflowInput(
    string RideId,
    IReadOnlyList<RidePassenger> Passengers,
    string RefundReason = "OperationalDecision");

/// <summary>Final output of a RideWorkflow instance.</summary>
/// <param name="Outcome">Human-readable outcome code, e.g. "Completed", "AbortedDueToPreFlightFailure".</param>
public sealed record RideWorkflowOutput(
    string RideId,
    RideStatus FinalStatus,
    string Outcome = "Completed",
    Guid? RefundBatchId = null);

