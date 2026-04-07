using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.StopRide;

public sealed class StopRideHandler(IRideStateStore store)
    : ICommandHandler<StopRideCommand, OperationResult>
{
    public async Task<OperationResult> HandleAsync(
        StopRideCommand command,
        CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(command.RideId, cancellationToken);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus == RideStatus.Maintenance)
            return OperationResult.Conflict("Ride is in Maintenance and cannot be stopped directly.");

        await store.SaveAsync(state.WithStatus(RideStatus.Idle), cancellationToken);
        return OperationResult.Success();
    }
}
