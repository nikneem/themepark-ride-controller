namespace ThemePark.Shared.Workflows;

/// <summary>
/// Factory for generating canonical Dapr Workflow instance IDs.
/// </summary>
public static class WorkflowIdFactory
{
    /// <summary>
    /// Creates a workflow instance ID using the format <c>ride-{rideId}-{yyyyMMddHHmmss}</c>.
    /// <para>
    /// Example: for ride <c>a1b2c3d4-0001-0000-0000-000000000001</c> started at UTC
    /// 2025-01-15 14:30:22 the resulting ID is
    /// <c>ride-a1b2c3d4-0001-0000-0000-000000000001-20250115143022</c>.
    /// </para>
    /// <para>
    /// The human-readable format enables tracing in the Dapr Dashboard during live demos.
    /// The UTC timestamp suffix prevents ID collisions when the same ride is restarted on
    /// the same day.
    /// </para>
    /// </summary>
    /// <param name="rideId">The stable GUID of the ride being started.</param>
    /// <param name="utcNow">The current UTC timestamp used to make the ID unique.</param>
    /// <returns>A workflow instance ID string safe for use with <c>DaprWorkflowClient.ScheduleNewWorkflowAsync</c>.</returns>
    public static string Create(Guid rideId, DateTime utcNow)
        => $"ride-{rideId}-{utcNow:yyyyMMddHHmmss}";
}
