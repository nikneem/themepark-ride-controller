namespace ThemePark.Mascots.Api.Models;

public sealed record MascotDto(
    string MascotId,
    string Name,
    string CurrentZone,
    bool IsInRestrictedZone,
    Guid? AffectedRideId);

public sealed record ClearMascotResponse(
    string MascotId,
    Guid ClearedFromRideId,
    DateTimeOffset ClearedAt);

public sealed record SimulateIntrusionRequest(
    string MascotId,
    string TargetRideId);
