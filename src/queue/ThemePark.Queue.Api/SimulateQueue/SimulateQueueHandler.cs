using Bogus;
using ThemePark.Queue.Api.Models;
using ThemePark.Queue.Models;
using ThemePark.Queue.State;

namespace ThemePark.Queue.Api.SimulateQueue;

/// <summary>
/// Demo-only: replaces the ride's queue with Bogus-generated passengers.
/// Gated by <c>Dapr:DemoMode</c> configuration.
/// </summary>
public sealed class SimulateQueueHandler(IQueueStateStore stateStore)
{
    public async Task<IResult> HandleAsync(string rideId, SimulateQueueRequest request, CancellationToken cancellationToken = default)
    {
        var faker = new Faker<Passenger>()
            .CustomInstantiator(f => new Passenger(
                PassengerId: Guid.NewGuid(),
                Name: f.Name.FullName(),
                IsVip: f.Random.Double() < request.VipProbability));

        var passengers = faker.Generate(request.Count);

        await stateStore.SavePassengersAsync(rideId, passengers, cancellationToken);

        return Results.Ok(new { seeded = passengers.Count, rideId });
    }
}
