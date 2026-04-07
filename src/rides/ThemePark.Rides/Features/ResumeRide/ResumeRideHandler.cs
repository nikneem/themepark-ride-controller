using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.ResumeRide;

public sealed class ResumeRideHandler(IRideStateStore store)
    : ICommandHandler<ResumeRideCommand, OperationResult>
{
    public async Task<OperationResult> HandleAsync(
        ResumeRideCommand command,
        CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(command.RideId, cancellationToken);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus != RideStatus.Paused)
            return OperationResult.Conflict($"Ride is in status '{state.OperationalStatus}'; expected Paused.");

        await store.SaveAsync(state.WithStatus(RideStatus.Running), cancellationToken);
        return OperationResult.Success();
    }
}
