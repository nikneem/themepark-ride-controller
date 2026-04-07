using Dapr.Client;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Infrastructure;

/// <summary>
/// Dapr state store adapter used by ControlCenter workflow activities.
/// Shares the same state store as the Ride Service (key: ride-state-{rideId}).
/// Reads and updates only the <see cref="RideStatus"/> within the full <see cref="RideState"/> JSON.
/// </summary>
public sealed class RideStateRepository(DaprClient daprClient) : IRideStateRepository
{
    private const string StoreName = "themepark-statestore";

    private static string Key(string rideId) => $"ride-state-{rideId}";

    public async Task<RideStatus> GetStatusAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var state = await daprClient.GetStateAsync<RideState?>(StoreName, Key(rideId), cancellationToken: cancellationToken);
        return state?.OperationalStatus ?? RideStatus.Idle;
    }

    public async Task SaveStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default)
    {
        var existing = await daprClient.GetStateAsync<RideState?>(StoreName, Key(rideId), cancellationToken: cancellationToken);

        var updated = existing is not null
            ? existing.WithStatus(status)
            : new RideState(Guid.TryParse(rideId, out var g) ? g : Guid.Empty, rideId, status, 0, 0, null);

        await daprClient.SaveStateAsync(StoreName, Key(rideId), updated, cancellationToken: cancellationToken);
    }
}

