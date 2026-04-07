using ThemePark.Shared.Enums;

namespace ThemePark.Shared.Tests;

public class RideStatusEnumTests
{
    [Fact]
    public void RideStatus_HasExactlyNineValues()
    {
        var values = Enum.GetValues<RideStatus>();
        Assert.Equal(9, values.Length);
    }

    [Theory]
    [InlineData(RideStatus.Idle, "Idle")]
    [InlineData(RideStatus.PreFlight, "PreFlight")]
    [InlineData(RideStatus.Loading, "Loading")]
    [InlineData(RideStatus.Running, "Running")]
    [InlineData(RideStatus.Paused, "Paused")]
    [InlineData(RideStatus.Maintenance, "Maintenance")]
    [InlineData(RideStatus.Resuming, "Resuming")]
    [InlineData(RideStatus.Completed, "Completed")]
    [InlineData(RideStatus.Failed, "Failed")]
    public void RideStatus_ValueHasCorrectName(RideStatus status, string expectedName)
    {
        Assert.Equal(expectedName, status.ToString());
    }
}
