using Dapr.Client;
using Dapr.Workflow;
using System.Net.Http.Json;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.ControlCenter.Domain;

namespace ThemePark.ControlCenter.Workflow.Activities;

/// <summary>Input for <see cref="RecordSessionSummaryActivity"/>.</summary>
public sealed record RecordSessionSummaryInput(
    Guid SessionId,
    Guid RideId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string Outcome);

/// <summary>
/// Starts the ride by calling <c>POST /rides/{rideId}/start</c> on rides-service via Dapr.
/// </summary>
public sealed class StartRideActivity : WorkflowActivity<string, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.RidesApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        var httpResponse = await HttpClient.PostAsync($"/rides/{rideId}/start", null);
        httpResponse.EnsureSuccessStatusCode();
        return true;
    }
}

/// <summary>
/// Removes the <c>active-workflow-{rideId}</c> state store entry when the workflow terminates,
/// preventing subsequent pub/sub events from raising external events into a finished workflow.
/// </summary>
public sealed class CleanupWorkflowActivity(DaprClient daprClient)
    : WorkflowActivity<string, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        await daprClient.DeleteStateAsync(AspireConstants.DaprComponents.StateStore, $"active-workflow-{rideId}");
        return true;
    }
}

/// <summary>
/// Persists a completed session summary to the Dapr state store and appends
/// the session ID to the per-ride session index so history queries can find it.
/// </summary>
public sealed class RecordSessionSummaryActivity(DaprClient daprClient)
    : WorkflowActivity<RecordSessionSummaryInput, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, RecordSessionSummaryInput input)
    {
        var session = new RideSession(
            input.SessionId,
            input.RideId,
            input.StartedAt,
            input.CompletedAt,
            input.Outcome);

        // Persist the session summary.
        await daprClient.SaveStateAsync(AspireConstants.DaprComponents.StateStore, $"session-summary-{input.SessionId}", session);

        // Append session ID to the ride's session index (last 20 kept).
        var index = await daprClient.GetStateAsync<List<Guid>?>(AspireConstants.DaprComponents.StateStore, $"sessions-{input.RideId}") ?? [];
        index.Add(input.SessionId);
        if (index.Count > 20)
            index = index[^20..];

        await daprClient.SaveStateAsync(AspireConstants.DaprComponents.StateStore, $"sessions-{input.RideId}", index);
        return true;
    }
}
