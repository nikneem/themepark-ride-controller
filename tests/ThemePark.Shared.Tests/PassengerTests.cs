using ThemePark.Shared.Records;

namespace ThemePark.Shared.Tests;

public class PassengerTests
{
    /// <summary>
    /// Verifies that the <see cref="Passenger"/> record does not expose any mutation path
    /// for <see cref="Passenger.IsVip"/> after construction.
    /// </summary>
    [Fact]
    public void Passenger_IsVip_IsImmutableAfterConstruction()
    {
        var passenger = new Passenger("p-001", "Alice", IsVip: true);

        // Records use init-only properties — the only way to "change" IsVip is via `with`,
        // which produces a *new* instance. The original must remain unchanged.
        var modified = passenger with { IsVip = false };

        Assert.True(passenger.IsVip, "Original passenger IsVip must remain true.");
        Assert.False(modified.IsVip, "New instance created via `with` has updated value.");
        Assert.NotSame(passenger, modified);
    }

    [Fact]
    public void Passenger_AllFieldsSetAtConstruction()
    {
        var id = "p-42";
        var passenger = new Passenger(id, "Bob", IsVip: false);

        Assert.Equal(id, passenger.PassengerId);
        Assert.Equal("Bob", passenger.Name);
        Assert.False(passenger.IsVip);
    }
}
