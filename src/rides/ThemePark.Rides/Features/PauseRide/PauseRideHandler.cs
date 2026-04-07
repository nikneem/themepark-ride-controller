using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Features.PauseRide;

public sealed record PauseRideCommand(string? Reason);

public sealed class PauseRideHandler(IRideStateStore store)
{
    public async Task<OperationResult> HandleAsync(string rideId, PauseRideCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return OperationResult.BadRequest("A pause reason is required.");

        var state = await store.GetAsync(rideId, ct);
        if (state is null)
            return OperationResult.NotFound();

        if (state.OperationalStatus != RideStatus.Running)
            return OperationResult.Conflict($"Ride is in status '{state.OperationalStatus}'; expected Running.");

        await store.SaveAsync(state.WithPause(command.Reason), ct);
        return OperationResult.Success();
    }
}
