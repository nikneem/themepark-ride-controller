using Dapr.Client;
using Dapr.Workflow;
using System.Net.Http.Json;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Workflow.Activities;

// Local DTOs for downstream service responses.
internal sealed record RideStateResult(Guid RideId, string Name, string OperationalStatus, int Capacity);
internal sealed record CurrentWeatherResult(string Severity, string[] AffectedZones, DateTimeOffset GeneratedAt);
internal sealed record MascotStatusResult(string MascotId, string Name, string CurrentZone, bool IsInRestrictedZone, Guid? AffectedRideId);
internal sealed record MaintenanceStatusResponse(bool IsOperational);
internal sealed record SafetyStatusResponse(bool SafetySystemsOk);

/// <summary>Result of a legacy pre-flight check activity.</summary>
public sealed record PreFlightCheckResult(bool IsHealthy, string Message);

/// <summary>
/// Checks the ride's current status via rides-service.
/// Throws <see cref="InvalidOperationException"/> if the ride is not <see cref="RideStatus.Idle"/>.
/// </summary>
public sealed class CheckRideStatusActivity : WorkflowActivity<string, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.RidesApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        var dto = await HttpClient.GetFromJsonAsync<RideStateResult>($"/rides/{rideId}")
            ?? throw new InvalidOperationException($"No response from rides-service for ride {rideId}.");

        if (!string.Equals(dto.OperationalStatus, RideStatus.Idle.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Ride {rideId} is not Idle (current status: {dto.OperationalStatus}). Cannot start workflow.");

        return true;
    }
}

/// <summary>
/// Pre-flight check: verifies weather conditions are safe for the ride.
/// Throws if severity is <see cref="WeatherSeverity.Severe"/>.
/// </summary>
public sealed class CheckWeatherActivity : WorkflowActivity<string, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.WeatherApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        var weather = await HttpClient.GetFromJsonAsync<CurrentWeatherResult>("/weather/current")
            ?? throw new InvalidOperationException("No response from weather-service.");

        if (string.Equals(weather.Severity, WeatherSeverity.Severe.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Weather severity is Severe — cannot start ride {rideId}.");

        return true;
    }
}

/// <summary>
/// Pre-flight check: verifies no mascot is in the ride's restricted zone.
/// Throws if a mascot with <c>IsInRestrictedZone == true</c> targets this ride.
/// </summary>
public sealed class CheckMascotActivity : WorkflowActivity<string, bool>
{
    private static readonly HttpClient HttpClient =
        DaprClient.CreateInvokeHttpClient(AspireConstants.Projects.MascotsApi);

    public override async Task<bool> RunAsync(WorkflowActivityContext context, string rideId)
    {
        var mascots = await HttpClient.GetFromJsonAsync<MascotStatusResult[]>("/mascots") ?? [];

        if (!Guid.TryParse(rideId, out var rideGuid))
            throw new ArgumentException($"Invalid rideId format: {rideId}");

        var intruder = mascots.FirstOrDefault(m => m.IsInRestrictedZone && m.AffectedRideId == rideGuid);
        if (intruder is not null)
            throw new InvalidOperationException(
                $"Mascot '{intruder.Name}' is in the restricted zone of ride {rideId}.");

        return true;
    }
}

// Legacy activities kept for backward compatibility — not used by the updated RideWorkflow.

/// <summary>Pre-flight check: verifies the ride is not under a maintenance block.</summary>
public sealed class CheckMascotZoneActivity(DaprClient daprClient)
    : WorkflowActivity<string, PreFlightCheckResult>
{
    public override async Task<PreFlightCheckResult> RunAsync(WorkflowActivityContext context, string rideId)
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<MascotStatusResult[]>(
                HttpMethod.Get, AspireConstants.Projects.MascotsApi, $"mascots");
            var hasIntruder = response?.Any(m => m.IsInRestrictedZone &&
                m.AffectedRideId == Guid.Parse(rideId)) == true;
            return new PreFlightCheckResult(!hasIntruder,
                hasIntruder ? "Mascot in restricted zone" : "Zone clear");
        }
        catch (Exception ex)
        {
            return new PreFlightCheckResult(false, $"Mascot zone check failed: {ex.Message}");
        }
    }
}

/// <summary>Pre-flight check: verifies the ride is not under a maintenance block.</summary>
public sealed class CheckMaintenanceStatusActivity(DaprClient daprClient)
    : WorkflowActivity<string, PreFlightCheckResult>
{
    public override async Task<PreFlightCheckResult> RunAsync(WorkflowActivityContext context, string rideId)
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<MaintenanceStatusResponse>(
                HttpMethod.Get, AspireConstants.Projects.MaintenanceApi, $"api/maintenance/rides/{rideId}/status");
            return new PreFlightCheckResult(response.IsOperational,
                response.IsOperational ? "Ride operational" : "Ride under maintenance");
        }
        catch (Exception ex)
        {
            return new PreFlightCheckResult(false, $"Maintenance status check failed: {ex.Message}");
        }
    }
}

/// <summary>Pre-flight check: verifies all ride safety systems report healthy.</summary>
public sealed class CheckSafetySystemsActivity(DaprClient daprClient)
    : WorkflowActivity<string, PreFlightCheckResult>
{
    public override async Task<PreFlightCheckResult> RunAsync(WorkflowActivityContext context, string rideId)
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<SafetyStatusResponse>(
                HttpMethod.Get, AspireConstants.Projects.RidesApi, $"api/rides/{rideId}/safety");
            return new PreFlightCheckResult(response.SafetySystemsOk,
                response.SafetySystemsOk ? "Safety systems OK" : "Safety system fault detected");
        }
        catch (Exception ex)
        {
            return new PreFlightCheckResult(false, $"Safety systems check failed: {ex.Message}");
        }
    }
}
