using Dapr.Client;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Infrastructure;

/// <summary>
/// Persists ride status in the Dapr state store.
/// Key format: <c>ride-state-{rideId}</c>. Returns <see cref="RideStatus.Idle"/> when no entry exists.
/// </summary>
public sealed class RideStateRepository(DaprClient daprClient) : IRideStateRepository
{
    private const string StoreName = "themepark-statestore";

    private static string Key(string rideId) => $"ride-state-{rideId}";

    public async Task<RideStatus> GetStatusAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var value = await daprClient.GetStateAsync<string?>(StoreName, Key(rideId), cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(value))
            return RideStatus.Idle;

        return Enum.TryParse<RideStatus>(value, out var status) ? status : RideStatus.Idle;
    }

    public async Task SaveStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        await daprClient.SaveStateAsync(StoreName, Key(rideId), status.ToString(), cancellationToken: cancellationToken);
    }
}
