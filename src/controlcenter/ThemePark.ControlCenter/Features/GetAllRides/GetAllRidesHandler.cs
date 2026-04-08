using Dapr.Client;
using Microsoft.Extensions.Logging;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.Rides.Abstractions.DataTransferObjects;

namespace ThemePark.ControlCenter.Features.GetAllRides;

/// <summary>
/// Retrieves all rides by invoking the rides-api via Dapr service invocation.
/// Returns an empty list if the invocation fails, to avoid breaking the control-center UI.
/// </summary>
public sealed class GetAllRidesHandler(DaprClient daprClient, ILogger<GetAllRidesHandler> logger)
{
    public async Task<IReadOnlyList<RideStateDto>> HandleAsync(
        GetAllRidesQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rides = await daprClient.InvokeMethodAsync<List<RideStateDto>>(
                HttpMethod.Get,
                AspireConstants.Projects.RidesApi,
                "/api/rides",
                cancellationToken);

            return rides ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve rides from rides-api. Returning empty list.");
            return [];
        }
    }
}
