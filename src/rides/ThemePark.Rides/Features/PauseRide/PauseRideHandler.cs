using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.PauseRide;

public sealed record PauseRideCommand(string RideId, string? Reason);

public sealed class PauseRideHandler(IRideStateStore store)
    : ICommandHandler<PauseRideCommand, OperationResult>
{
    public async Task<OperationResult> HandleAsync(
        PauseRideCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return OperationResult.BadRequest("A pause reason is required.");

        var state = await store.GetAsync(command.RideId, cancellationToken);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus != RideStatus.Running)
            return OperationResult.Conflict($"Ride is in status '{state.OperationalStatus}'; expected Running.");

        await store.SaveAsync(state.WithPause(command.Reason), cancellationToken);
        return OperationResult.Success();
    }
}
