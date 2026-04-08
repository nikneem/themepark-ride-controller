namespace ThemePark.ControlCenter.Tests.Activities;

/// <summary>
/// Workflow activities in <see cref="ThemePark.ControlCenter.Workflow.Activities"/> use either:
/// <list type="bullet">
///   <item>Static <c>DaprClient.CreateInvokeHttpClient()</c> HTTP clients (e.g.
///     <c>IssueRefundActivity</c>, <c>PauseRideActivity</c>, <c>ResumeRideActivity</c>), which
///     are not replaceable without modifying production code.</item>
///   <item><c>WorkflowActivityContext</c> as their entry-point parameter, which is sealed in
///     the Dapr.Workflow SDK and cannot be substituted.</item>
/// </list>
/// Isolated unit testing of these activities is therefore impractical without
/// significant test-surface refactoring.
///
/// Coverage for workflow activities is achieved through integration tests in
/// <c>ThemePark.IntegrationTests</c> using <c>Aspire.Hosting.Testing</c>, which exercises
/// the full workflow including all activities against a real Dapr sidecar.
/// </summary>
internal static class ActivityCoverageNotes;
