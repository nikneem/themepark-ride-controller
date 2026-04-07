using ThemePark.Shared.Events;

namespace ThemePark.Shared.Tests;

public class IntegrationEventTests
{
    private sealed record TestEvent(string EventId, DateTimeOffset OccurredAt)
        : IntegrationEvent(EventId, OccurredAt);

    [Fact]
    public void IntegrationEvent_SubclassInheritsEventId()
    {
        var evt = new TestEvent("evt-001", DateTimeOffset.UtcNow);
        Assert.Equal("evt-001", evt.EventId);
    }

    [Fact]
    public void IntegrationEvent_SubclassInheritsOccurredAt()
    {
        var timestamp = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var evt = new TestEvent("evt-002", timestamp);
        Assert.Equal(timestamp, evt.OccurredAt);
    }

    [Fact]
    public void IntegrationEvent_ValueEqualityBasedOnProperties()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var evt1 = new TestEvent("evt-003", timestamp);
        var evt2 = new TestEvent("evt-003", timestamp);
        Assert.Equal(evt1, evt2);
    }
}
