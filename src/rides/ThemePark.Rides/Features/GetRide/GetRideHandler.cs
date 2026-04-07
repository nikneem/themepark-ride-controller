using ThemePark.Rides.Abstractions.DataTransferObjects;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Rides.Features.GetRide;

public sealed class GetRideHandler(IRideStateStore store)
    : IQueryHandler<GetRideQuery, OperationResult<RideStateDto>>
{
    public async Task<OperationResult<RideStateDto>> HandleAsync(
        GetRideQuery query,
        CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(query.RideId, cancellationToken);
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
