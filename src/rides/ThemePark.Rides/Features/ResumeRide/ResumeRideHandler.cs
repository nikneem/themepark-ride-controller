using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.ResumeRide;

public sealed class ResumeRideHandler(IRideStateStore store)
{
    public async Task<OperationResult> HandleAsync(string rideId, CancellationToken ct = default)
    {
        var state = await store.GetAsync(rideId, ct);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus != RideStatus.Paused)
            return OperationResult.Conflict($"Ride is in status '{state.OperationalStatus}'; expected Paused.");

        await store.SaveAsync(state.WithStatus(RideStatus.Running), ct);
        return OperationResult.Success();
    }
}
