using ThemePark.Rides.Api._Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.PauseRide;

/// <summary>
/// Transitions a ride from <see cref="RideStatus.Running"/> to <see cref="RideStatus.Paused"/>.
/// Returns 400 if reason is missing, 404 if not found, 409 if not Running.
/// </summary>
public sealed class PauseRideHandler(IRideStateStore store)
{
    public async Task<IResult> HandleAsync(string rideId, PauseRideRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.BadRequest(new { error = "A pause reason is required." });

        var state = await store.GetAsync(rideId, cancellationToken);
        if (state is null)
            return Results.NotFound();

        if (state.OperationalStatus != RideStatus.Running)
            return Results.Conflict(new { error = $"Ride is in status '{state.OperationalStatus}'; expected Running." });

        await store.SaveAsync(state.WithPause(request.Reason), cancellationToken);
        return Results.Ok();
    }
}
