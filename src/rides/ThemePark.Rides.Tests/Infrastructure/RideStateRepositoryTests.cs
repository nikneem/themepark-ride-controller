using Dapr.Client;
using NSubstitute;
using ThemePark.Rides.Api.Infrastructure;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared.Enums;

namespace ThemePark.Rides.Tests.Infrastructure;

/// <summary>
/// Unit tests for RideStateRepository covering the key format and missing-key-defaults-to-Idle behaviour.
/// </summary>
public sealed class RideStateRepositoryTests
{
    private const string StoreName = "themepark-statestore";

    [Fact]
    public async Task GetStatusAsync_MissingKey_ReturnsIdle()
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string?>(StoreName, "ride-state-new-ride", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var repo = new RideStateRepository(daprClient);
        var status = await repo.GetStatusAsync("new-ride");

        Assert.Equal(RideStatus.Idle, status);
    }

    [Fact]
    public async Task GetStatusAsync_EmptyString_ReturnsIdle()
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string?>(StoreName, "ride-state-x", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var repo = new RideStateRepository(daprClient);
        var status = await repo.GetStatusAsync("x");

        Assert.Equal(RideStatus.Idle, status);
    }

    [Theory]
    [InlineData("PreFlight",   RideStatus.PreFlight)]
    [InlineData("Loading",     RideStatus.Loading)]
    [InlineData("Running",     RideStatus.Running)]
    [InlineData("Paused",      RideStatus.Paused)]
    [InlineData("Maintenance", RideStatus.Maintenance)]
    [InlineData("Resuming",    RideStatus.Resuming)]
    [InlineData("Completed",   RideStatus.Completed)]
    [InlineData("Failed",      RideStatus.Failed)]
    public async Task GetStatusAsync_StoredValue_ReturnsCorrectStatus(string stored, RideStatus expected)
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string?>(StoreName, "ride-state-r1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(stored);

        var repo = new RideStateRepository(daprClient);
        var status = await repo.GetStatusAsync("r1");

        Assert.Equal(expected, status);
    }

    [Fact]
    public async Task GetStatusAsync_UsesCorrectKeyFormat()
    {
        var daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<string?>(StoreName, "ride-state-coaster-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns("Running");

        var repo = new RideStateRepository(daprClient);
        await repo.GetStatusAsync("coaster-1");

        await daprClient.Received(1)
            .GetStateAsync<string?>(StoreName, "ride-state-coaster-1", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveStatusAsync_WritesStringRepresentationToCorrectKey()
    {
        var daprClient = Substitute.For<DaprClient>();
        var repo = new RideStateRepository(daprClient);

        await repo.SaveStatusAsync("ride-42", RideStatus.Running);

        await daprClient.Received(1)
            .SaveStateAsync(StoreName, "ride-state-ride-42", "Running", cancellationToken: Arg.Any<CancellationToken>());
    }
}
