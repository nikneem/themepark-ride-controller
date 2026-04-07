using ThemePark.Queue.Api.Models;
using ThemePark.Queue.State;

namespace ThemePark.Queue.Api.GetQueue;

/// <summary>
/// Returns the current queue state for a ride.
/// Returns zeroed response when no queue exists.
/// </summary>
public sealed class GetQueueHandler(IQueueStateStore stateStore, IConfiguration configuration)
{
    public async Task<IResult> HandleAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var avgLoadCapacity = configuration.GetValue<double>("Queue:AverageLoadCapacity", 20);
        var avgRideDuration = configuration.GetValue<double>("Queue:AverageRideDurationMinutes", 3);

        var passengers = await stateStore.GetPassengersAsync(rideId, cancellationToken);

        if (passengers.Count == 0)
        {
            return Results.Ok(new QueueStateResponse(rideId, 0, false, 0));
        }

        var waitingCount = passengers.Count;
        var hasVip = passengers.Any(p => p.IsVip);
        var estimatedWait = avgLoadCapacity > 0
            ? waitingCount / avgLoadCapacity * avgRideDuration
            : 0;

        return Results.Ok(new QueueStateResponse(rideId, waitingCount, hasVip, Math.Round(estimatedWait, 2)));
    }
}
