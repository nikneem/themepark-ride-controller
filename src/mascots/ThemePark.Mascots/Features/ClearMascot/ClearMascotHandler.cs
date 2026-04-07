using Dapr.Client;
using ThemePark.EventContracts.Events;
using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.State;
using ThemePark.Mascots.Zones;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Mascots.Features.ClearMascot;

public sealed class ClearMascotHandler(IMascotStateStore store, DaprClient daprClient)
    : ICommandHandler<ClearMascotCommand, OperationResult<ClearMascotResponse>>
{
    public async Task<OperationResult<ClearMascotResponse>> HandleAsync(
        ClearMascotCommand command,
        CancellationToken cancellationToken = default)
    {
        var mascot = store.GetById(command.MascotId);

        if (mascot is null)
            return OperationResult<ClearMascotResponse>.NotFound();

        if (!mascot.IsInRestrictedZone || mascot.AffectedRideId is null)
            return OperationResult<ClearMascotResponse>.NotFound();

        var clearedFromRideId = mascot.AffectedRideId.Value;
        var clearedAt = DateTimeOffset.UtcNow;

        store.TryUpdateZone(command.MascotId, MascotZones.ParkCentral, out _);

        var evt = new MascotClearedEvent(command.MascotId, clearedFromRideId, clearedAt);
        await daprClient.PublishEventAsync("themepark-pubsub", "mascot.cleared", evt, cancellationToken);

        return OperationResult<ClearMascotResponse>.Success(
            new ClearMascotResponse(command.MascotId, clearedFromRideId, clearedAt));
    }
}
