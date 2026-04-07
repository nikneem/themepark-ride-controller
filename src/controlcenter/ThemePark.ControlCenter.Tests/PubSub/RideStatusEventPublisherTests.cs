using Dapr.Client;
using NSubstitute;
using ThemePark.ControlCenter.PubSub;
using ThemePark.EventContracts.Events;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Tests.PubSub;

/// <summary>
/// Verifies RideStatusEventPublisher payload shape and correct pub/sub topic targeting.
/// </summary>
public sealed class RideStatusEventPublisherTests
{
    private const string PubSubName = "themepark-pubsub";
    private const string TopicName  = "ride.status-changed";

    [Fact]
    public async Task PublishAsync_SendsToCorrectPubSubAndTopic()
    {
        var daprClient = Substitute.For<DaprClient>();
        var publisher = new RideStatusEventPublisher(daprClient);

        await publisher.PublishAsync("ride-1", RideStatus.Idle, RideStatus.PreFlight, "StartPreFlightActivity");

        await daprClient.Received(1)
            .PublishEventAsync(
                PubSubName,
                TopicName,
                Arg.Any<RideStatusChangedEvent>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_EventPayload_HasCorrectStatusStrings()
    {
        RideStatusChangedEvent? captured = null;
        var daprClient = Substitute.For<DaprClient>();
        await daprClient.PublishEventAsync(
            PubSubName, TopicName,
            Arg.Do<RideStatusChangedEvent>(e => captured = e),
            Arg.Any<CancellationToken>());

        var publisher = new RideStatusEventPublisher(daprClient);
        await publisher.PublishAsync("ride-1", RideStatus.Idle, RideStatus.PreFlight, "StartPreFlightActivity");

        Assert.NotNull(captured);
        Assert.Equal("Idle",                   captured.PreviousStatus);
        Assert.Equal("PreFlight",               captured.NewStatus);
        Assert.Equal("StartPreFlightActivity",  captured.WorkflowStep);
    }

    [Fact]
    public async Task PublishAsync_EventPayload_ChangedAtIsUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        RideStatusChangedEvent? captured = null;
        var daprClient = Substitute.For<DaprClient>();
        await daprClient.PublishEventAsync(
            PubSubName, TopicName,
            Arg.Do<RideStatusChangedEvent>(e => captured = e),
            Arg.Any<CancellationToken>());

        var publisher = new RideStatusEventPublisher(daprClient);
        await publisher.PublishAsync("ride-1", RideStatus.Idle, RideStatus.PreFlight, null);

        Assert.NotNull(captured);
        Assert.True(captured.ChangedAt >= before);
    }

    [Fact]
    public async Task PublishAsync_NullWorkflowStep_SetsEmptyString()
    {
        RideStatusChangedEvent? captured = null;
        var daprClient = Substitute.For<DaprClient>();
        await daprClient.PublishEventAsync(
            PubSubName, TopicName,
            Arg.Do<RideStatusChangedEvent>(e => captured = e),
            Arg.Any<CancellationToken>());

        var publisher = new RideStatusEventPublisher(daprClient);
        await publisher.PublishAsync("ride-1", RideStatus.Idle, RideStatus.PreFlight, null);

        Assert.NotNull(captured);
        Assert.Equal(string.Empty, captured.WorkflowStep);
    }
}
