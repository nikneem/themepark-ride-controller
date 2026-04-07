namespace ThemePark.Rides.Api.PauseRide;

/// <summary>Request body for pausing a ride.</summary>
public sealed record PauseRideRequest(string? Reason);
