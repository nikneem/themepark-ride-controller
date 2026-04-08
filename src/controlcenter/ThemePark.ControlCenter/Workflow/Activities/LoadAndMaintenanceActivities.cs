using Dapr.Client;
using Dapr.Workflow;
using System.Net.Http.Json;
using ThemePark.Aspire.ServiceDefaults;

namespace ThemePark.ControlCenter.Workflow.Activities;

// Local DTOs for queue and maintenance service responses.
internal sealed record PassengerItem(Guid PassengerId, string Name, bool IsVip);
internal sealed record LoadPassengersServiceRequest(int Capacity);
internal sealed record LoadPassengersServiceResponse(
    IReadOnlyList<PassengerItem> Passengers,
    int LoadedCount,
    int VipCount,
    int RemainingInQueue);
internal sealed record CreateMaintenanceRequest(
    Guid RideId,
    string Reason,
    string? WorkflowId,
    DateTimeOffset RequestedAt);

/// <summary>Output from <see cref="LoadPassengersActivity"/>.</summary>
public sealed record LoadPassengersResult(
    IReadOnlyList<RidePassenger> Passengers,
    int VipCount,
    bool HasVip);

/// <summary>
/// Loads passengers from the queue service for the ride.
/// Calls <c>POST /queue/{rideId}/load</c> and returns the loaded passengers.
/// </summary>
public sealed class LoadPassengersActivity : WorkflowActivity<string, LoadPassengersResult>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.QueueApi);

    public override async Task<LoadPassengersResult> RunAsync(WorkflowActivityContext context, string rideId)
    {
        // Use a default capacity of 20; the rides-service knows the real capacity
        // but loading passengers does not require it to be exact here.
        var request = new LoadPassengersServiceRequest(Capacity: 20);
        var httpResponse = await HttpClient.PostAsJsonAsync($"/queue/{rideId}/load", request);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<LoadPassengersServiceResponse>()
            ?? throw new InvalidOperationException($"Empty response from queue-service for ride {rideId}.");

        var passengers = response.Passengers
            .Select(p => new RidePassenger(p.PassengerId.ToString(), p.IsVip))
            .ToList();

        return new LoadPassengersResult(passengers, response.VipCount, response.VipCount > 0);
    }
}

/// <summary>
/// Triggers a maintenance request via the maintenance-service.
/// Calls <c>POST /maintenance</c> with ride and workflow context.
/// </summary>
public sealed class TriggerMaintenanceActivity : WorkflowActivity<TriggerMaintenanceActivityInput, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.MaintenanceApi);

    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        TriggerMaintenanceActivityInput input)
    {
        var request = new CreateMaintenanceRequest(
            input.RideId,
            input.Reason,
            input.WorkflowId,
            DateTimeOffset.UtcNow);

        var httpResponse = await HttpClient.PostAsJsonAsync("/maintenance", request);
        httpResponse.EnsureSuccessStatusCode();
        return true;
    }
}
