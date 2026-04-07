using ThemePark.Rides.Models;

namespace ThemePark.Rides.Infrastructure;

public interface IRideStateStore
{
    Task<RideState?> GetAsync(string rideId, CancellationToken cancellationToken = default);
    Task SaveAsync(RideState state, CancellationToken cancellationToken = default);
}
