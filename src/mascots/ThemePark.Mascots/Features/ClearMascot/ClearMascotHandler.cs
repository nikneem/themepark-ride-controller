using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.State;
using ThemePark.Mascots.Zones;
using ThemePark.Shared;

namespace ThemePark.Mascots.Features.ClearMascot;

public sealed class ClearMascotHandler(IMascotStateStore store, DaprClient daprClient)
{
    public async Task<OperationResult<ClearMascotResponse>> HandleAsync(string mascotId, CancellationToken ct = default)
    {
        var mascot = store.GetById(mascotId);

        if (mascot is null)
            return OperationResult<ClearMascotResponse>.NotFound();

        if (!mascot.IsInRestrictedZone || mascot.AffectedRideId is null)
            return OperationResult<ClearMascotResponse>.NotFound();

        var clearedFromRideId = mascot.AffectedRideId.Value;
        var clearedAt = DateTimeOffset.UtcNow;

        store.TryUpdateZone(mascotId, MascotZones.ParkCentral, out _);

        var evt = new MascotClearedEvent(mascotId, clearedFromRideId, clearedAt);
        await daprClient.PublishEventAsync("themepark-pubsub", "mascot.cleared", evt, ct);

        return OperationResult<ClearMascotResponse>.Success(
            new ClearMascotResponse(mascotId, clearedFromRideId, clearedAt));
    }
}
