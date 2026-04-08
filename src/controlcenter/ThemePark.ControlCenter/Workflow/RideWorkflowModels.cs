using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow;

// Legacy transition types — used by old state-machine activities kept for backward compatibility.
/// <summary>Input for legacy workflow activities that trigger a ride state transition.</summary>
public sealed record RideTransitionInput(string RideId, RideStatus TargetStatus);

/// <summary>Output from legacy workflow activities after a successful state transition.</summary>
public sealed record RideTransitionOutput(string RideId, RideStatus NewStatus);

/// <summary>A passenger on a ride, used for refund calculation on failure.</summary>
public sealed record RidePassenger(string PassengerId, bool IsVip);

/// <summary>Input for <see cref="Activities.PauseRideActivity"/>.</summary>
public sealed record PauseRideActivityInput(string RideId, string Reason);

/// <summary>Input for <see cref="Activities.TriggerMaintenanceActivity"/>.</summary>
public sealed record TriggerMaintenanceActivityInput(Guid RideId, string WorkflowId, string Reason);

/// <summary>Input to start a RideWorkflow instance.</summary>
public sealed record RideWorkflowInput(
    string RideId,
    string WorkflowId,
    DateTimeOffset StartedAt,
    int RideDurationSeconds = 90);

/// <summary>Final output of a RideWorkflow instance.</summary>
/// <param name="Outcome">Human-readable outcome code, e.g. "Completed", "AbortedDueToPreFlightFailure".</param>
public sealed record RideWorkflowOutput(
    string RideId,
    RideStatus FinalStatus,
    string Outcome = "Completed",
    Guid? RefundBatchId = null);

