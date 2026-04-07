using ThemePark.Queue.Models;

namespace ThemePark.Queue.State;

public interface IQueueStateStore
{
    Task<IReadOnlyList<Passenger>> GetPassengersAsync(string rideId, CancellationToken cancellationToken = default);
    Task SavePassengersAsync(string rideId, IReadOnlyList<Passenger> passengers, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Passenger> Passengers, string ETag)> GetPassengersWithETagAsync(string rideId, CancellationToken cancellationToken = default);
    Task<bool> TrySavePassengersAsync(string rideId, IReadOnlyList<Passenger> passengers, string eTag, CancellationToken cancellationToken = default);
}
