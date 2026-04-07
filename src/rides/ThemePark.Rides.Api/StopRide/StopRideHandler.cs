using ThemePark.Rides.Api._Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Api.StopRide;

/// <summary>
/// Transitions a ride to <see cref="RideStatus.Idle"/> from any state except <see cref="RideStatus.Maintenance"/>.
/// Returns 404 if not found, 409 if in Maintenance.
/// </summary>
public sealed class StopRideHandler(IRideStateStore store)
{
    public async Task<IResult> HandleAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(rideId, cancellationToken);
        if (state is null)
            return Results.NotFound();

        if (state.OperationalStatus == RideStatus.Maintenance)
            return Results.Conflict(new { error = "Ride is in Maintenance and cannot be stopped directly." });

        await store.SaveAsync(state.WithStatus(RideStatus.Idle), cancellationToken);
        return Results.Ok();
    }
}
