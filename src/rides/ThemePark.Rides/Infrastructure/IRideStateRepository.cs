using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Infrastructure;

/// <summary>
/// Reads and writes the current <see cref="RideStatus"/> for a ride from the Dapr state store.
/// Key format: <c>ride-state-{rideId}</c>. A missing key resolves to <see cref="RideStatus.Idle"/>.
/// </summary>
public interface IRideStateRepository
{
    Task<RideStatus> GetStatusAsync(string rideId, CancellationToken cancellationToken = default);
    Task SaveStatusAsync(string rideId, RideStatus status, CancellationToken cancellationToken = default);
}
