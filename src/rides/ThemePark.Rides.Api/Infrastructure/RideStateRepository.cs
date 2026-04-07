using Dapr.Client;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Infrastructure;

/// <summary>
/// Persists ride status in the Dapr state store by reading and updating the full <see cref="RideState"/> object.
/// Key format: <c>ride-state-{rideId}</c>. Returns <see cref="RideStatus.Idle"/> when no entry exists.
/// </summary>
public sealed class RideStateRepository(DaprClient daprClient) : IRideStateRepository
{
    private static string Key(string rideId) => $"ride-state-{rideId}";

    public async Task<RideStatus> GetStatusAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var state = await daprClient.GetStateAsync<RideState?>(
            DaprRideStateStore.StoreName, Key(rideId), cancellationToken: cancellationToken);
        return state?.OperationalStatus ?? RideStatus.Idle;
    }

    public async Task SaveStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        var existing = await daprClient.GetStateAsync<RideState?>(
            DaprRideStateStore.StoreName, Key(rideId), cancellationToken: cancellationToken);

        var updated = existing is not null
            ? existing.WithStatus(status)
            : new RideState(Guid.TryParse(rideId, out var g) ? g : Guid.Empty, rideId, status, 0, 0, null);

        await daprClient.SaveStateAsync(
            DaprRideStateStore.StoreName, Key(rideId), updated, cancellationToken: cancellationToken);
    }
}

