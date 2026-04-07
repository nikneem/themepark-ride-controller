using Bogus;
using Dapr.Client;
using ThemePark.Queue.Models;
using ThemePark.Queue.Api.Models;

namespace ThemePark.Queue.Api.SimulateQueue;

/// <summary>
/// Demo-only: replaces the ride's queue with Bogus-generated passengers.
/// Gated by <c>Dapr:DemoMode</c> configuration (task 5.1).
/// </summary>
public sealed class SimulateQueueHandler(DaprClient daprClient)
{
    private const string StoreName = "themepark-statestore";

    public async Task<IResult> HandleAsync(string rideId, SimulateQueueRequest request, CancellationToken cancellationToken = default)
    {
        var faker = new Faker<Passenger>()
            .CustomInstantiator(f => new Passenger(
                PassengerId: Guid.NewGuid(),
                Name: f.Name.FullName(),
                IsVip: f.Random.Double() < request.VipProbability));

        var passengers = faker.Generate(request.Count);

        await daprClient.SaveStateAsync(StoreName, $"queue-{rideId}", passengers, cancellationToken: cancellationToken);

        return Results.Ok(new { seeded = passengers.Count, rideId });
    }
}
