using ThemePark.Rides.Api._Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.ResumeRide;

/// <summary>
/// Transitions a ride from <see cref="RideStatus.Paused"/> to <see cref="RideStatus.Running"/>.
/// Returns 404 if not found, 409 if not Paused.
/// </summary>
public sealed class ResumeRideHandler(IRideStateStore store)
{
    public async Task<IResult> HandleAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(rideId, cancellationToken);
        if (state is null)
            return Results.NotFound();

        if (state.OperationalStatus != RideStatus.Paused)
            return Results.Conflict(new { error = $"Ride is in status '{state.OperationalStatus}'; expected Paused." });

        await store.SaveAsync(state.WithStatus(RideStatus.Running), cancellationToken);
        return Results.Ok();
    }
}
