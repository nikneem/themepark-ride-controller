using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.StartRide;

public sealed class StartRideHandler(IRideStateStore store)
    : ICommandHandler<StartRideCommand, OperationResult>
{
    public async Task<OperationResult> HandleAsync(
        StartRideCommand command,
        CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(command.RideId, cancellationToken);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus != RideStatus.Idle)
            return OperationResult.Conflict($"Ride is in status '{state.OperationalStatus}'; expected Idle.");

        await store.SaveAsync(state.WithStatus(RideStatus.Running), cancellationToken);
        return OperationResult.Success();
    }
}
