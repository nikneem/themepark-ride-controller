namespace ThemePark.Queue.Features.SimulateQueue;

public sealed record SimulateQueueCommand(string RideId, int Count, double VipProbability = 0.1);
