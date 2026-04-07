using Bogus;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;
using ThemePark.Shared;

namespace ThemePark.Queue.Features.SimulateQueue;

public sealed class SimulateQueueHandler(IQueueStateStore stateStore)
{
    public async Task<OperationResult<SimulateQueueResponse>> HandleAsync(
        string rideId,
        SimulateQueueRequest request,
        CancellationToken ct = default)
    {
        var faker = new Faker<Passenger>()
            .CustomInstantiator(f => new Passenger(
                PassengerId: Guid.NewGuid(),
                Name: f.Name.FullName(),
                IsVip: f.Random.Double() < request.VipProbability));

        var passengers = faker.Generate(request.Count);
        await stateStore.SavePassengersAsync(rideId, passengers, ct);

        return OperationResult<SimulateQueueResponse>.Success(
            new SimulateQueueResponse(passengers.Count, rideId));
    }
}
