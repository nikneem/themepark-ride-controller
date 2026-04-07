using ThemePark.Mascots.Api.Models;
using ThemePark.Mascots.Api.State;

namespace ThemePark.Mascots.Api.GetMascots;

public sealed class GetMascotsHandler(MascotStateStore store)
{
    public IReadOnlyList<MascotDto> Handle() =>
        store.GetAll()
             .Select(m => new MascotDto(m.MascotId, m.Name, m.CurrentZone, m.IsInRestrictedZone, m.AffectedRideId))
             .ToList();
}
