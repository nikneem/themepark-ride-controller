namespace ThemePark.Queue.Api.Models;

/// <summary>Current state of a ride's passenger queue.</summary>
public sealed record QueueStateResponse(
    string RideId,
    int WaitingCount,
    bool HasVip,
    double EstimatedWaitMinutes);

/// <summary>Request to load passengers from the queue onto a ride.</summary>
public sealed record LoadPassengersRequest(int Capacity);

/// <summary>Response containing the boarded passengers and queue summary.</summary>
public sealed record LoadPassengersResponse(
    IReadOnlyList<PassengerDto> Passengers,
    int LoadedCount,
    int VipCount,
    int RemainingInQueue);

/// <summary>DTO for a single passenger in a response.</summary>
public sealed record PassengerDto(Guid PassengerId, string Name, bool IsVip);

/// <summary>Request to seed a ride queue with simulated passengers.</summary>
public sealed record SimulateQueueRequest(int Count, double VipProbability = 0.1);
