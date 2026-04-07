using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.State;
using ThemePark.Shared;

namespace ThemePark.Queue.Features.LoadPassengers;

public sealed class LoadPassengersHandler(IQueueStateStore stateStore)
{
    private const int MaxRetries = 5;

    public async Task<OperationResult<LoadPassengersResponse>> HandleAsync(
        string rideId,
        LoadPassengersRequest request,
        CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var (passengers, etag) = await stateStore.GetPassengersWithETagAsync(rideId, ct);

            var queue = passengers.ToList();
            var toLoad = queue.Take(request.Capacity).ToList();
            var remainder = queue.Skip(request.Capacity).ToList();

            var saved = await stateStore.TrySavePassengersAsync(rideId, remainder, etag, ct);

            if (!saved) continue;

            var response = new LoadPassengersResponse(
                Passengers: toLoad.Select(p => new PassengerDto(p.PassengerId, p.Name, p.IsVip)).ToList(),
                LoadedCount: toLoad.Count,
                VipCount: toLoad.Count(p => p.IsVip),
                RemainingInQueue: remainder.Count);

            return OperationResult<LoadPassengersResponse>.Success(response);
        }

        return OperationResult<LoadPassengersResponse>.Conflict(
            "Could not atomically load passengers after maximum retries. Please try again.");
    }
}
