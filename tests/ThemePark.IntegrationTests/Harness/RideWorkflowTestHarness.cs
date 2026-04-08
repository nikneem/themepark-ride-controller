using System.Net.Http.Json;
using ThemePark.Shared.Enums;

namespace ThemePark.IntegrationTests.Harness;

public sealed class RideWorkflowTestHarness
{
    private readonly HttpClient _httpClient;

    public RideWorkflowTestHarness(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> StartRideAsync(string rideId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"/controlcenter/rides/{rideId}/start", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StartRideResult>(ct);
        return result!.WorkflowInstanceId;
    }

    public async Task<RideStatus?> GetStateAsync(string rideId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/controlcenter/rides/{rideId}/status", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RideStatusResult>(ct);
        return result is not null && Enum.TryParse<RideStatus>(result.Status, true, out var s) ? s : null;
    }

    public async Task WaitForStateAsync(string rideId, RideStatus targetState, TimeSpan timeout, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        while (!cts.Token.IsCancellationRequested)
        {
            var current = await GetStateAsync(rideId, cts.Token);
            if (current == targetState)
                return;
            await Task.Delay(500, cts.Token);
        }
        throw new WorkflowStateTimeoutException(rideId, targetState.ToString());
    }

    private sealed record StartRideResult(string WorkflowInstanceId);
    private sealed record RideStatusResult(string RideId, string Name, string Status);
}
