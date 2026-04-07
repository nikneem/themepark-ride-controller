using Microsoft.Extensions.Configuration;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Queue.Features.GetQueue;

public sealed class GetQueueHandler(IQueueStateStore stateStore, IConfiguration configuration)
    : IQueryHandler<GetQueueQuery, OperationResult<QueueStateResponse>>
{
    public async Task<OperationResult<QueueStateResponse>> HandleAsync(
        GetQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        var avgLoadCapacity = configuration.GetValue<double>("Queue:AverageLoadCapacity", 20);
        var avgRideDuration = configuration.GetValue<double>("Queue:AverageRideDurationMinutes", 3);

        var passengers = await stateStore.GetPassengersAsync(query.RideId, cancellationToken);

        if (passengers.Count == 0)
            return OperationResult<QueueStateResponse>.Success(new QueueStateResponse(query.RideId, 0, false, 0));

        var waitingCount = passengers.Count;
        var hasVip = passengers.Any(p => p.IsVip);
        var estimatedWait = avgLoadCapacity > 0
            ? waitingCount / avgLoadCapacity * avgRideDuration
            : 0;

        return OperationResult<QueueStateResponse>.Success(
            new QueueStateResponse(query.RideId, waitingCount, hasVip, Math.Round(estimatedWait, 2)));
    }
}
