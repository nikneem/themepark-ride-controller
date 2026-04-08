namespace ThemePark.IntegrationTests.Harness;

public sealed class WorkflowStateTimeoutException : Exception
{
    public WorkflowStateTimeoutException(string rideId, string expectedState)
        : base($"Workflow for ride '{rideId}' did not reach state '{expectedState}' within the timeout.") { }
}
