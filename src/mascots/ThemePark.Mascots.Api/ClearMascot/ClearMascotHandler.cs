using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Api.Models;
using ThemePark.Mascots.Api.State;
using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Api.ClearMascot;

public sealed class ClearMascotHandler(MascotStateStore store, DaprClient daprClient)
{
    public async Task<IResult> HandleAsync(string mascotId, CancellationToken ct = default)
    {
        var mascot = store.GetById(mascotId);

        if (mascot is null)
            return Results.NotFound();

        if (!mascot.IsInRestrictedZone || mascot.AffectedRideId is null)
            return Results.NotFound();

        var clearedFromRideId = mascot.AffectedRideId.Value;
        var clearedAt = DateTimeOffset.UtcNow;

        store.TryUpdateZone(mascotId, MascotZones.ParkCentral, out _);

        var evt = new MascotClearedEvent(mascotId, clearedFromRideId, clearedAt);
        await daprClient.PublishEventAsync("themepark-pubsub", "mascot.cleared", evt, ct);

        return Results.Ok(new ClearMascotResponse(mascotId, clearedFromRideId, clearedAt));
    }
}
