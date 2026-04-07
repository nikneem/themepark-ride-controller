namespace ThemePark.Shared.Events;

public abstract record IntegrationEvent(string EventId, DateTimeOffset OccurredAt);
