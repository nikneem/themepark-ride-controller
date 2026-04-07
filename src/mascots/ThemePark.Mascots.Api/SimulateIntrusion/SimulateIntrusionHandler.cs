using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Api.Models;
using ThemePark.Mascots.State;
using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Api.SimulateIntrusion;

public sealed class SimulateIntrusionHandler(IMascotStateStore store, DaprClient daprClient)
{
    public async Task<IResult> HandleAsync(SimulateIntrusionRequest request, CancellationToken ct = default)
    {
        var mascot = store.GetById(request.MascotId);
        if (mascot is null)
            return Results.BadRequest($"Unknown mascot: {request.MascotId}");

        var targetZone = MascotZones.GetZoneForRideId(request.TargetRideId);
        if (targetZone is null)
            return Results.BadRequest($"Unknown or non-restricted ride zone: {request.TargetRideId}");

        store.TryUpdateZone(request.MascotId, targetZone, out var updated);

        if (updated is { IsInRestrictedZone: true, AffectedRideId: not null })
        {
            var evt = new MascotInRestrictedZoneEvent(
                Guid.NewGuid(),
                updated.MascotId,
                updated.Name,
                updated.AffectedRideId.Value,
                DateTimeOffset.UtcNow);

            await daprClient.PublishEventAsync(
                "themepark-pubsub", "mascot.in-restricted-zone", evt, ct);
        }

        return Results.Accepted();
    }
}
