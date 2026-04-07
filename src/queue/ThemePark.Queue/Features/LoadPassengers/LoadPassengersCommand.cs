namespace ThemePark.Queue.Features.LoadPassengers;

public sealed record LoadPassengersCommand(string RideId, int Capacity);
