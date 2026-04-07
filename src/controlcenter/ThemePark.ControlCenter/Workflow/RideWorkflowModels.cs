using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow;

/// <summary>Input for workflow activities that trigger a ride state transition.</summary>
public sealed record RideTransitionInput(string RideId, RideStatus TargetStatus);

/// <summary>Output from workflow activities after a successful state transition.</summary>
public sealed record RideTransitionOutput(string RideId, RideStatus NewStatus);
