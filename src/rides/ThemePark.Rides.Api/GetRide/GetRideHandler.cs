using ThemePark.Rides.Api._Shared;

namespace ThemePark.Rides.Api.GetRide;

/// <summary>Returns the current state of a ride, or 404 if not found.</summary>
public sealed class GetRideHandler(IRideStateStore store)
{
    public async Task<IResult> HandleAsync(string rideId, CancellationToken cancellationToken = default)
    {
        var state = await store.GetAsync(rideId, cancellationToken);
        return state is null
            ? Results.NotFound()
            : Results.Ok(RideStateResponse.From(state));
    }
}
