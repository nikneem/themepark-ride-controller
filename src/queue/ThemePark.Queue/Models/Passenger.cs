namespace ThemePark.Queue.Models;

/// <summary>A passenger waiting in the queue for a ride.</summary>
public sealed record Passenger(Guid PassengerId, string Name, bool IsVip);
