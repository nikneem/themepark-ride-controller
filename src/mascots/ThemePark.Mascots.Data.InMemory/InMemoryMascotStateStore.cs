using System.Collections.Concurrent;
using ThemePark.Mascots.Models;
using ThemePark.Mascots.State;
using ThemePark.Mascots.Zones;

namespace ThemePark.Mascots.Data.InMemory;

internal sealed record MascotState(string MascotId, string Name, string CurrentZone);

/// <summary>
/// In-memory implementation of <see cref="IMascotStateStore"/>.
/// State resets on every restart by design — mascots are not persisted.
/// </summary>
public sealed class InMemoryMascotStateStore : IMascotStateStore
{
    private readonly ConcurrentDictionary<string, MascotState> _mascots;

    public InMemoryMascotStateStore()
    {
        _mascots = new ConcurrentDictionary<string, MascotState>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in InitialMascots())
            _mascots[m.MascotId] = m;
    }

    private static IEnumerable<MascotState> InitialMascots() =>
    [
        new("mascot-001", "Roary the Lion 🦁", MascotZones.ParkCentral),
        new("mascot-002", "Bella the Bear 🐻", MascotZones.ParkCentral),
        new("mascot-003", "Ziggy the Zebra 🦓", MascotZones.ParkCentral),
    ];

    public IReadOnlyList<Mascot> GetAll() =>
        _mascots.Values.Select(ToMascot).ToList();

    public Mascot? GetById(string mascotId) =>
        _mascots.TryGetValue(mascotId, out var m) ? ToMascot(m) : null;

    public bool TryUpdateZone(string mascotId, string newZone, out Mascot? updated)
    {
        if (!_mascots.TryGetValue(mascotId, out var current))
        {
            updated = null;
            return false;
        }
        var newState = current with { CurrentZone = newZone };
        _mascots[mascotId] = newState;
        updated = ToMascot(newState);
        return true;
    }

    /// <summary>Returns true if any mascot is in <paramref name="zone"/>.</summary>
    public bool IsZoneOccupied(string zone) =>
        _mascots.Values.Any(m => m.CurrentZone.Equals(zone, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns true if a mascot OTHER than <paramref name="excludeMascotId"/> is in <paramref name="zone"/>.</summary>
    public bool IsZoneOccupiedByOther(string zone, string excludeMascotId) =>
        _mascots.Values.Any(m =>
            !m.MascotId.Equals(excludeMascotId, StringComparison.OrdinalIgnoreCase) &&
            m.CurrentZone.Equals(zone, StringComparison.OrdinalIgnoreCase));

    private static Mascot ToMascot(MascotState s)
    {
        var rideId = MascotZones.GetRideId(s.CurrentZone);
        return new Mascot(s.MascotId, s.Name, s.CurrentZone, rideId.HasValue, rideId);
    }
}
