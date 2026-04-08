using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using ThemePark.ControlCenter.Features;
using ThemePark.Rides.Abstractions.DataTransferObjects;

namespace ThemePark.ControlCenter.Features.GetRideStatus;

/// <summary>
/// Retrieves the current status of a single ride by invoking the rides-api via Dapr service invocation.
/// Returns null when the ride is not found (404).
/// </summary>
public sealed class GetRideStatusHandler(DaprClient daprClient, ILogger<GetRideStatusHandler> logger)
{
    public async Task<RideStatusResponse?> HandleAsync(
        GetRideStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await daprClient.InvokeMethodAsync<RideStateDto>(
                HttpMethod.Get,
                "rides-api",
                $"/api/rides/{query.RideId}",
                cancellationToken);

            if (dto is null)
                return null;

            return new RideStatusResponse(
                dto.RideId,
                dto.Name,
                dto.OperationalStatus,
                WorkflowStep: null,
                ActiveChaosEvents: []);
        }
        catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve status for ride {RideId} from rides-api.", query.RideId);
            return null;
        }
    }
}
