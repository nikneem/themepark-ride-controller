using Dapr.Client;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;

namespace ThemePark.Rides.Data.Dapr;

/// <summary>
/// Dapr state store implementation of <see cref="IRideStateStore"/>.
/// Reads and writes the full <see cref="RideState"/> JSON at key <c>ride-state-{rideId}</c>.
/// </summary>
public sealed class DaprRideStateStore(DaprClient daprClient) : IRideStateStore
{
    public const string StoreName = AspireConstants.DaprComponents.StateStore;

    private static string Key(string rideId) => $"ride-state-{rideId}";

    public Task<RideState?> GetAsync(string rideId, CancellationToken cancellationToken = default) =>
        daprClient.GetStateAsync<RideState?>(StoreName, Key(rideId), cancellationToken: cancellationToken);

    public Task SaveAsync(RideState state, CancellationToken cancellationToken = default) =>
        daprClient.SaveStateAsync(StoreName, Key(state.RideId.ToString()), state, cancellationToken: cancellationToken);
}
