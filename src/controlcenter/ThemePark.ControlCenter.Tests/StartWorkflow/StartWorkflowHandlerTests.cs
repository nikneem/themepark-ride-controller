using NSubstitute;
using ThemePark.ControlCenter.Features.StartWorkflow;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Tests.StartWorkflow;

public sealed class StartWorkflowHandlerTests
{
    private readonly IRideStateRepository _rideStateRepository = Substitute.For<IRideStateRepository>();

    private StartWorkflowHandler CreateHandler() =>
        new(_rideStateRepository, null!, null!);

    private static readonly string RideId = "a1b2c3d4-0001-0000-0000-000000000001";

    /// <summary>
    /// Verifies the at-most-one-workflow guard: when a ride's status is not Idle, the handler
    /// returns 409 Conflict without reaching the DaprWorkflowClient.
    /// </summary>
    [Theory]
    [InlineData(RideStatus.Running)]
    [InlineData(RideStatus.Loading)]
    [InlineData(RideStatus.PreFlight)]
    [InlineData(RideStatus.Paused)]
    [InlineData(RideStatus.Maintenance)]
    public async Task StartRide_WhenAlreadyActive_Returns409(RideStatus activeStatus)
    {
        _rideStateRepository
            .GetStatusAsync(RideId, Arg.Any<CancellationToken>())
            .Returns(activeStatus);

        var result = await CreateHandler().HandleAsync(
            new StartWorkflowCommand(RideId));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
    }

    [Fact]
    public async Task StartRide_InvalidRideIdFormat_ReturnsBadRequest()
    {
        var result = await CreateHandler().HandleAsync(
            new StartWorkflowCommand("not-a-guid"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }
}
