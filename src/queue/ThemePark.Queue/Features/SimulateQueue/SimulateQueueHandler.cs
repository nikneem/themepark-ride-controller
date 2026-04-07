using Bogus;
using ThemePark.Queue.Abstractions.DataTransferObjects;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;
using ThemePark.Shared;
using ThemePark.Shared.Cqrs;

namespace ThemePark.Queue.Features.SimulateQueue;

public sealed class SimulateQueueHandler(IQueueStateStore stateStore)
    : ICommandHandler<SimulateQueueCommand, OperationResult<SimulateQueueResponse>>
{
    public async Task<OperationResult<SimulateQueueResponse>> HandleAsync(
        SimulateQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        var faker = new Faker<Passenger>()
            .CustomInstantiator(f => new Passenger(
                PassengerId: Guid.NewGuid(),
                Name: f.Name.FullName(),
                IsVip: f.Random.Double() < command.VipProbability));

        var passengers = faker.Generate(command.Count);
        await stateStore.SavePassengersAsync(command.RideId, passengers, cancellationToken);

        return OperationResult<SimulateQueueResponse>.Success(
            new SimulateQueueResponse(passengers.Count, command.RideId));
    }
}
