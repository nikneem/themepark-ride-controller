namespace ThemePark.Rides.Tests.Activities;

/// <summary>
/// Workflow activities (<see cref="ThemePark.ControlCenter.Workflow.Activities"/>) use
/// static Dapr HTTP clients and Dapr Workflow context, making isolated unit testing
/// impractical without significant test-surface refactoring.
///
/// Coverage for workflow activities is achieved through integration tests in
/// <c>ThemePark.IntegrationTests</c> using <c>Aspire.Hosting.Testing</c>.
/// </summary>
internal static class ActivityCoverageNotes;
