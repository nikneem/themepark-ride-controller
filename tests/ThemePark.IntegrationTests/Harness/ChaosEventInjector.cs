namespace ThemePark.IntegrationTests.Harness;

public sealed class ChaosEventInjector
{
    private readonly HttpClient _httpClient;

    public ChaosEventInjector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task WeatherMildAsync(string rideId, CancellationToken ct = default)
        => RaiseEventAsync(rideId, "weather-alert", "Mild", ct);

    public Task WeatherSevereAsync(string rideId, CancellationToken ct = default)
        => RaiseEventAsync(rideId, "weather-alert", "Severe", ct);

    public Task MascotIntrusionAsync(string rideId, CancellationToken ct = default)
        => RaiseEventAsync(rideId, "mascot-in-restricted-zone", rideId, ct);

    public Task MechanicalFailureAsync(string rideId, CancellationToken ct = default)
        => RaiseEventAsync(rideId, "ride-malfunction", rideId, ct);

    public Task ClearWeatherAsync(string rideId, string eventId, CancellationToken ct = default)
        => ResolveEventAsync(rideId, eventId, "WeatherMild", ct);

    public Task ClearIntrusionAsync(string rideId, string eventId, CancellationToken ct = default)
        => ResolveEventAsync(rideId, eventId, "MascotIntrusion", ct);

    public async Task ApproveMaintenanceAsync(string rideId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"/controlcenter/rides/{rideId}/maintenance/approve", null, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task RaiseEventAsync(string rideId, string eventType, string payload, CancellationToken ct)
    {
        var content = System.Net.Http.Json.JsonContent.Create(new { rideId, payload });
        var response = await _httpClient.PostAsync($"/{eventType}", content, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task ResolveEventAsync(string rideId, string eventId, string eventType, CancellationToken ct)
    {
        var response = await _httpClient.PostAsync(
            $"/controlcenter/rides/{rideId}/events/{eventId}/resolve?eventType={eventType}", null, ct);
        response.EnsureSuccessStatusCode();
    }
}
