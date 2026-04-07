using Microsoft.Extensions.Configuration;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.State;
using ThemePark.Shared;

namespace ThemePark.Queue.Features.GetQueue;

public sealed class GetQueueHandler(IQueueStateStore stateStore, IConfiguration configuration)
{
    public async Task<OperationResult<QueueStateResponse>> HandleAsync(string rideId, CancellationToken ct = default)
    {
        var avgLoadCapacity = configuration.GetValue<double>("Queue:AverageLoadCapacity", 20);
        var avgRideDuration = configuration.GetValue<double>("Queue:AverageRideDurationMinutes", 3);

        var passengers = await stateStore.GetPassengersAsync(rideId, ct);

        if (passengers.Count == 0)
            return OperationResult<QueueStateResponse>.Success(new QueueStateResponse(rideId, 0, false, 0));

        var waitingCount = passengers.Count;
        var hasVip = passengers.Any(p => p.IsVip);
        var estimatedWait = avgLoadCapacity > 0
            ? waitingCount / avgLoadCapacity * avgRideDuration
            : 0;

        return OperationResult<QueueStateResponse>.Success(
            new QueueStateResponse(rideId, waitingCount, hasVip, Math.Round(estimatedWait, 2)));
    }
}
