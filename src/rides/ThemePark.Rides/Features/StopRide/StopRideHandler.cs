using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.StopRide;

public sealed class StopRideHandler(IRideStateStore store)
{
    public async Task<OperationResult> HandleAsync(string rideId, CancellationToken ct = default)
    {
        var state = await store.GetAsync(rideId, ct);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus == RideStatus.Maintenance)
            return OperationResult.Conflict("Ride is in Maintenance and cannot be stopped directly.");

        await store.SaveAsync(state.WithStatus(RideStatus.Idle), ct);
        return OperationResult.Success();
    }
}
