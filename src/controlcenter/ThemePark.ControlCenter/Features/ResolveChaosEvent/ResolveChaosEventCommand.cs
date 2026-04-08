namespace ThemePark.ControlCenter.Features.ResolveChaosEvent;

/// <summary>
/// Command to resolve an active chaos event for a ride session.
/// EventType must be one of: "WeatherAlert", "MascotIntrusion", "RideMalfunction".
/// </summary>
public sealed record ResolveChaosEventCommand(string RideId, string EventId, string EventType);
