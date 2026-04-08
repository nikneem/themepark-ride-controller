using Dapr.Client;
using Dapr.Workflow;

namespace ThemePark.ControlCenter.Workflow.Activities;

/// <summary>Result of a pre-flight check activity.</summary>
public sealed record PreFlightCheckResult(bool IsHealthy, string Message);

// Response DTOs from downstream services.
internal sealed record WeatherCheckResponse(bool IsSafe);
internal sealed record MascotZoneCheckResponse(bool MascotPresent);
internal sealed record MaintenanceStatusResponse(bool IsOperational);
internal sealed record SafetyStatusResponse(bool SafetySystemsOk);

/// <summary>Pre-flight check: verifies weather conditions are safe for the ride.</summary>
public sealed class CheckWeatherActivity(DaprClient daprClient)
    : WorkflowActivity<string, PreFlightCheckResult>
{
    public override async Task<PreFlightCheckResult> RunAsync(WorkflowActivityContext context, string rideId)
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<WeatherCheckResponse>(
                HttpMethod.Get, "weather-api", "api/weather/current");
            return new PreFlightCheckResult(response.IsSafe, response.IsSafe ? "Weather is safe" : "Weather conditions unsafe");
        }
        catch (Exception ex)
        {
            return new PreFlightCheckResult(false, $"Weather check failed: {ex.Message}");
        }
    }
}

/// <summary>Pre-flight check: verifies no mascot is in the ride's restricted zone.</summary>
public sealed class CheckMascotZoneActivity(DaprClient daprClient)
    : WorkflowActivity<string, PreFlightCheckResult>
{
    public override async Task<PreFlightCheckResult> RunAsync(WorkflowActivityContext context, string rideId)
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<MascotZoneCheckResponse>(
                HttpMethod.Get, "mascots-api", $"api/mascots/restricted-zone/{rideId}");
            return new PreFlightCheckResult(!response.MascotPresent,
                response.MascotPresent ? "Mascot in restricted zone" : "Zone clear");
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
                HttpMethod.Get, "maintenance-api", $"api/maintenance/rides/{rideId}/status");
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
                HttpMethod.Get, "rides-api", $"api/rides/{rideId}/safety");
            return new PreFlightCheckResult(response.SafetySystemsOk,
                response.SafetySystemsOk ? "Safety systems OK" : "Safety system fault detected");
        }
        catch (Exception ex)
        {
            return new PreFlightCheckResult(false, $"Safety systems check failed: {ex.Message}");
        }
    }
}
