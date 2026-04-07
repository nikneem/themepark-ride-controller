using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.StateMachine;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.Features.Rides;

/// <summary>
/// Handles ride lifecycle commands by reading state, applying the transition, and persisting the result.
/// </summary>
public sealed class RideCommandHandlers(IRideStateRepository repository)
{
    public async Task<RideStatus> TransitionAsync(
        string rideId,
        RideStatus targetStatus,
        CancellationToken cancellationToken = default)
    {
        var currentStatus = await repository.GetStatusAsync(rideId, cancellationToken);
        var machine = new RideStateMachine(rideId, currentStatus);

        machine.Transition(targetStatus);

        await repository.SaveStatusAsync(rideId, machine.CurrentStatus, cancellationToken);

        machine.ClearEvents();
        return machine.CurrentStatus;
    }
}
