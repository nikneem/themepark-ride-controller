using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThemePark.ControlCenter.Features;
using ThemePark.ControlCenter.Features.GetRideHistory;

namespace ThemePark.ControlCenter.Tests.GetRideHistory;

public sealed class GetRideHistoryHandlerTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly GetRideHistoryHandler _handler;

    public GetRideHistoryHandlerTests()
    {
        _handler = new GetRideHistoryHandler(_daprClient, NullLogger<GetRideHistoryHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SessionIdsExist_ReturnsHistoryEntries()
    {
        var rideId = "test-ride-1";
        var sessionIds = new List<string> { "session-1", "session-2" };
        var entry1 = new RideHistoryEntry(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-30), DateTimeOffset.UtcNow.AddMinutes(-2), "Completed");
        var entry2 = new RideHistoryEntry(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1), "Completed");

        _daprClient
            .GetStateAsync<List<string>?>(Arg.Any<string>(), $"sessions-{rideId}", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(sessionIds);

        _daprClient
            .GetStateAsync<RideHistoryEntry?>(Arg.Any<string>(), "session-summary-session-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry1);

        _daprClient
            .GetStateAsync<RideHistoryEntry?>(Arg.Any<string>(), "session-summary-session-2", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry2);

        var result = await _handler.HandleAsync(new GetRideHistoryQuery(rideId));

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_NoSessionIds_ReturnsEmptyList()
    {
        _daprClient
            .GetStateAsync<List<string>?>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns((List<string>?)null);

        var result = await _handler.HandleAsync(new GetRideHistoryQuery("any-ride-id"));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_EmptySessionIdList_ReturnsEmptyList()
    {
        _daprClient
            .GetStateAsync<List<string>?>(Arg.Any<string>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var result = await _handler.HandleAsync(new GetRideHistoryQuery("any-ride-id"));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_SessionSummaryMissing_SkipsEntry()
    {
        var rideId = "test-ride-1";
        var sessionIds = new List<string> { "session-found", "session-missing" };
        var entry = new RideHistoryEntry(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-30), DateTimeOffset.UtcNow.AddMinutes(-2), "Completed");

        _daprClient
            .GetStateAsync<List<string>?>(Arg.Any<string>(), $"sessions-{rideId}", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(sessionIds);

        _daprClient
            .GetStateAsync<RideHistoryEntry?>(Arg.Any<string>(), "session-summary-session-found", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);

        _daprClient
            .GetStateAsync<RideHistoryEntry?>(Arg.Any<string>(), "session-summary-session-missing", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((RideHistoryEntry?)null);

        var result = await _handler.HandleAsync(new GetRideHistoryQuery(rideId));

        // Only the found entry is included; missing entries are skipped with a warning.
        Assert.Single(result);
    }
}
