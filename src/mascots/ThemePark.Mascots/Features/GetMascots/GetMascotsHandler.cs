using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.State;
using ThemePark.Shared;

namespace ThemePark.Mascots.Features.GetMascots;

public sealed class GetMascotsHandler(IMascotStateStore store)
{
    public OperationResult<IReadOnlyList<MascotDto>> Handle() =>
        OperationResult<IReadOnlyList<MascotDto>>.Success(
            store.GetAll()
                 .Select(m => new MascotDto(m.MascotId, m.Name, m.CurrentZone, m.IsInRestrictedZone, m.AffectedRideId))
                 .ToList());
}
