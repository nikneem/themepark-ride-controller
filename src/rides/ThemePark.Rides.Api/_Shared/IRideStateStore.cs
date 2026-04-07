using ThemePark.Rides.Models;

namespace ThemePark.Rides.Api._Shared;

/// <summary>
/// Abstraction for all Dapr state store reads and writes for the full <see cref="RideState"/> object.
/// Key format: <c>ride-state-{rideId}</c>.
/// </summary>
public interface IRideStateStore
{
    Task<RideState?> GetAsync(string rideId, CancellationToken cancellationToken = default);
    Task SaveAsync(RideState state, CancellationToken cancellationToken = default);
}
