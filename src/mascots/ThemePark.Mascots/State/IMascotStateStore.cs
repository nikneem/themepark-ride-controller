using ThemePark.Mascots.Models;

namespace ThemePark.Mascots.State;

public interface IMascotStateStore
{
    IReadOnlyList<Mascot> GetAll();
    Mascot? GetById(string mascotId);
    bool TryUpdateZone(string mascotId, string newZone, out Mascot? updated);
    bool IsZoneOccupied(string zone);
    bool IsZoneOccupiedByOther(string zone, string excludeMascotId);
}
