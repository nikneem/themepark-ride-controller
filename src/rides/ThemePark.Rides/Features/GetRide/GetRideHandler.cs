using ThemePark.Rides.Abstractions.DataTransferObjects;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;

namespace ThemePark.Rides.Features.GetRide;

public sealed class GetRideHandler(IRideStateStore store)
{
    public async Task<OperationResult<RideStateDto>> HandleAsync(string rideId, CancellationToken ct = default)
    {
        var state = await store.GetAsync(rideId, ct);
        if (state is null)
            return OperationResult<RideStateDto>.NotFound();

        return OperationResult<RideStateDto>.Success(new RideStateDto(
            state.RideId,
            state.Name,
            state.OperationalStatus.ToString(),
            state.Capacity,
            state.CurrentPassengerCount,
            state.PauseReason));
    }
}
