using ThemePark.Queue.Api.Models;
using ThemePark.Queue.State;

namespace ThemePark.Queue.Api.LoadPassengers;

/// <summary>
/// Atomically dequeues up to <c>capacity</c> passengers from a ride queue.
/// Uses ETag concurrency with up to 5 retries.
/// </summary>
public sealed class LoadPassengersHandler(IQueueStateStore stateStore)
{
    private const int MaxRetries = 5;

    public async Task<IResult> HandleAsync(string rideId, LoadPassengersRequest request, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var (passengers, etag) = await stateStore.GetPassengersWithETagAsync(rideId, cancellationToken);

            var queue = passengers.ToList();
            var toLoad = queue.Take(request.Capacity).ToList();
            var remainder = queue.Skip(request.Capacity).ToList();

            var saved = await stateStore.TrySavePassengersAsync(rideId, remainder, etag, cancellationToken);

            if (!saved) continue;

            var response = new LoadPassengersResponse(
                Passengers: toLoad.Select(p => new PassengerDto(p.PassengerId, p.Name, p.IsVip)).ToList(),
                LoadedCount: toLoad.Count,
                VipCount: toLoad.Count(p => p.IsVip),
                RemainingInQueue: remainder.Count);

            return Results.Ok(response);
        }

        return Results.Conflict(new { error = "Could not atomically load passengers after maximum retries. Please try again." });
    }
}
