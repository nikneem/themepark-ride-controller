using Dapr.Client;
using NSubstitute;
using ThemePark.Rides.Api.Infrastructure;
using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Infrastructure;
using ThemePark.Rides.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Infrastructure;

/// <summary>
/// Unit tests for RideStateRepository. The repository reads/writes full <see cref="RideState"/> JSON;
/// status-only operations extract/patch the <see cref="RideState.OperationalStatus"/> field.
/// </summary>
public sealed class RideStateRepositoryTests
{
    private const string StoreName = DaprRideStateStore.StoreName;

    private static RideState MakeState(RideStatus status) => new(
        Guid.NewGuid(), "Test Ride", status, 10, 0, null);

    [Fact]
    public async Task GetStatusAsync_MissingKey_ReturnsIdle()
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<RideState?>(StoreName, "ride-state-new-ride", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((RideState?)null);

        var repo = new RideStateRepository(daprClient);
        var status = await repo.GetStatusAsync("new-ride");

        Assert.Equal(RideStatus.Idle, status);
    }

    [Theory]
    [InlineData(RideStatus.PreFlight)]
    [InlineData(RideStatus.Loading)]
    [InlineData(RideStatus.Running)]
    [InlineData(RideStatus.Paused)]
    [InlineData(RideStatus.Maintenance)]
    [InlineData(RideStatus.Resuming)]
    [InlineData(RideStatus.Completed)]
    [InlineData(RideStatus.Failed)]
    public async Task GetStatusAsync_StoredState_ReturnsCorrectStatus(RideStatus expected)
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<RideState?>(StoreName, "ride-state-r1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(MakeState(expected));

        var repo = new RideStateRepository(daprClient);
        var status = await repo.GetStatusAsync("r1");

        Assert.Equal(expected, status);
    }

    [Fact]
    public async Task GetStatusAsync_UsesCorrectKeyFormat()
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<RideState?>(StoreName, "ride-state-coaster-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(MakeState(RideStatus.Running));

        var repo = new RideStateRepository(daprClient);
        await repo.GetStatusAsync("coaster-1");

        await daprClient.Received(1)
            .GetStateAsync<RideState?>(StoreName, "ride-state-coaster-1", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveStatusAsync_ExistingState_PreservesNameAndCapacity()
    {
        var existing = new RideState(Guid.NewGuid(), "Thunder Mountain", RideStatus.Idle, 24, 0, null);
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<RideState?>(StoreName, "ride-state-ride-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(existing);

        RideState? saved = null;
        await daprClient.SaveStateAsync(StoreName, "ride-state-ride-1",
            Arg.Do<RideState>(s => saved = s), cancellationToken: Arg.Any<CancellationToken>());

        var repo = new RideStateRepository(daprClient);
        await repo.SaveStatusAsync("ride-1", RideStatus.Running);

        Assert.NotNull(saved);
        Assert.Equal(RideStatus.Running, saved.OperationalStatus);
        Assert.Equal("Thunder Mountain", saved.Name);
        Assert.Equal(24, saved.Capacity);
    }

    [Fact]
    public async Task SaveStatusAsync_MissingState_CreatesPlaceholder()
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<RideState?>(StoreName, Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns((RideState?)null);

        RideState? saved = null;
        await daprClient.SaveStateAsync(StoreName, Arg.Any<string>(),
            Arg.Do<RideState>(s => saved = s), cancellationToken: Arg.Any<CancellationToken>());

        var repo = new RideStateRepository(daprClient);
        await repo.SaveStatusAsync("ride-42", RideStatus.Running);

        Assert.NotNull(saved);
        Assert.Equal(RideStatus.Running, saved.OperationalStatus);
    }
}

