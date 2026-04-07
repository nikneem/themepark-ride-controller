using Dapr.Client;
using ThemePark.Queue.Models;
using ThemePark.Queue.Api.Models;

namespace ThemePark.Queue.Api.LoadPassengers;

/// <summary>
/// Atomically dequeues up to <c>capacity</c> passengers from a ride queue.
/// Uses Dapr ETag concurrency with up to 5 retries (task 4.1).
/// </summary>
public sealed class LoadPassengersHandler(DaprClient daprClient)
{
    private const string StoreName = "themepark-statestore";
    private const int MaxRetries = 5;

    public async Task<IResult> HandleAsync(string rideId, LoadPassengersRequest request, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var (passengers, etag) = await daprClient.GetStateAndETagAsync<List<Passenger>?>(
                StoreName, $"queue-{rideId}", cancellationToken: cancellationToken);

            var queue = passengers ?? [];
            var toLoad = queue.Take(request.Capacity).ToList();
            var remainder = queue.Skip(request.Capacity).ToList();

            var saved = await daprClient.TrySaveStateAsync(
                StoreName, $"queue-{rideId}", remainder, etag, cancellationToken: cancellationToken);

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
