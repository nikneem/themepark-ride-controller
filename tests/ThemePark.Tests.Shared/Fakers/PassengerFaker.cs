using Bogus;
using ThemePark.Shared.Records;

namespace ThemePark.Tests.Shared.Fakers;

public sealed class PassengerFaker : Faker<Passenger>
{
    public PassengerFaker()
    {
        var envSeed = Environment.GetEnvironmentVariable("BOGUS_SEED");
        if (int.TryParse(envSeed, out var seed))
            UseSeed(seed);

        CustomInstantiator(f => new Passenger(
            PassengerId: f.Random.Guid().ToString(),
            Name: f.Name.FullName(),
            IsVip: f.Random.Bool(0.2f)));
    }
}
