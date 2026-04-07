namespace ThemePark.Queue.Abstractions.DataTransferObjects;

public sealed record QueueStateResponse(
    string RideId,
    int WaitingCount,
    bool HasVip,
    double EstimatedWaitMinutes);

public sealed record LoadPassengersRequest(int Capacity);

public sealed record PassengerDto(Guid PassengerId, string Name, bool IsVip);

public sealed record LoadPassengersResponse(
    IReadOnlyList<PassengerDto> Passengers,
    int LoadedCount,
    int VipCount,
    int RemainingInQueue);

public sealed record SimulateQueueRequest(int Count, double VipProbability = 0.1);

public sealed record SimulateQueueResponse(int Seeded, string RideId);
