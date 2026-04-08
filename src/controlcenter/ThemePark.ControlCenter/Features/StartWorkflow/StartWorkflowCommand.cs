namespace ThemePark.ControlCenter.Features.StartWorkflow;

/// <summary>Command to start a new ride workflow session.</summary>
public sealed record StartWorkflowCommand(
    string RideId,
    int RideDurationSeconds = 90);
