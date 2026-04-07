using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Queue.Features.LoadPassengers;

public sealed class LoadPassengersHandler(IQueueStateStore stateStore)
    : ICommandHandler<LoadPassengersCommand, OperationResult<LoadPassengersResponse>>
{
    private const int MaxRetries = 5;

    public async Task<OperationResult<LoadPassengersResponse>> HandleAsync(
        LoadPassengersCommand command,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var (passengers, etag) = await stateStore.GetPassengersWithETagAsync(command.RideId, cancellationToken);

            var queue = passengers.ToList();
            var toLoad = queue.Take(command.Capacity).ToList();
            var remainder = queue.Skip(command.Capacity).ToList();

            var saved = await stateStore.TrySavePassengersAsync(command.RideId, remainder, etag, cancellationToken);

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
