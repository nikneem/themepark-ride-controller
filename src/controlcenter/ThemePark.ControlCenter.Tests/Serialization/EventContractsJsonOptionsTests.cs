using System.Text.Json;
using ThemePark.EventContracts.Events;
using ThemePark.EventContracts.Serialization;
using ThemePark.Shared.Enums;

namespace ThemePark.ControlCenter.Tests.Serialization;

/// <summary>
/// Verifies that <see cref="EventContractsJsonOptions.Default"/> produces camelCase property names
/// and string enum values (camelCase) for all event contracts.
/// </summary>
public sealed class EventContractsJsonOptionsTests
{
    [Fact]
    public void WeatherAlertEvent_Serializes_With_CamelCase_Properties()
    {
        var evt = new WeatherAlertEvent(
            EventId: Guid.NewGuid(),
            Severity: WeatherSeverity.Mild,
            AffectedZones: ["Zone-A", "Zone-B"],
            GeneratedAt: DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);

        Assert.Contains("\"eventId\"", json);
        Assert.Contains("\"severity\"", json);
        Assert.Contains("\"affectedZones\"", json);
        Assert.Contains("\"generatedAt\"", json);
    }

    [Fact]
    public void WeatherAlertEvent_Serializes_Enum_As_CamelCase_String()
    {
        var mildEvt = new WeatherAlertEvent(Guid.NewGuid(), WeatherSeverity.Mild, ["Zone-A"], DateTimeOffset.UtcNow);
        var severeEvt = new WeatherAlertEvent(Guid.NewGuid(), WeatherSeverity.Severe, ["Zone-B"], DateTimeOffset.UtcNow);
        var calmEvt = new WeatherAlertEvent(Guid.NewGuid(), WeatherSeverity.Calm, [], DateTimeOffset.UtcNow);

        Assert.Contains("\"mild\"", JsonSerializer.Serialize(mildEvt, EventContractsJsonOptions.Default));
        Assert.Contains("\"severe\"", JsonSerializer.Serialize(severeEvt, EventContractsJsonOptions.Default));
        Assert.Contains("\"calm\"", JsonSerializer.Serialize(calmEvt, EventContractsJsonOptions.Default));
    }

    [Fact]
    public void WeatherAlertEvent_DoesNotContain_Numeric_Enum_Values()
    {
        var evt = new WeatherAlertEvent(Guid.NewGuid(), WeatherSeverity.Severe, ["Zone-C"], DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);

        // Enum should appear as string "severe", not its integer value 2
        Assert.DoesNotContain("\"severity\":2", json);
        Assert.Contains("\"severe\"", json);
    }

    [Fact]
    public void RideStatusChangedEvent_Serializes_With_CamelCase_Properties()
    {
        var evt = new RideStatusChangedEvent(
            RideId: Guid.NewGuid(),
            PreviousStatus: "Idle",
            NewStatus: "PreFlight",
            WorkflowStep: "StartPreFlightActivity",
            ChangedAt: DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);

        Assert.Contains("\"rideId\"", json);
        Assert.Contains("\"previousStatus\"", json);
        Assert.Contains("\"newStatus\"", json);
        Assert.Contains("\"workflowStep\"", json);
        Assert.Contains("\"changedAt\"", json);
    }

    [Fact]
    public void RideMalfunctionEvent_Serializes_With_CamelCase_Properties()
    {
        var evt = new RideMalfunctionEvent(
            EventId: Guid.NewGuid(),
            RideId: Guid.NewGuid(),
            RideName: "Thunder Mountain",
            FaultCode: "E001",
            Description: "Sensor failure",
            OccurredAt: DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);

        Assert.Contains("\"eventId\"", json);
        Assert.Contains("\"rideId\"", json);
        Assert.Contains("\"rideName\"", json);
        Assert.Contains("\"faultCode\"", json);
        Assert.Contains("\"description\"", json);
        Assert.Contains("\"occurredAt\"", json);
    }

    [Fact]
    public void MaintenanceRequestedEvent_Serializes_With_CamelCase_Properties()
    {
        var evt = new MaintenanceRequestedEvent(
            EventId: Guid.NewGuid(),
            MaintenanceId: Guid.NewGuid().ToString(),
            RideId: Guid.NewGuid(),
            Reason: "MechanicalFailure",
            RequestedAt: DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);

        Assert.Contains("\"maintenanceId\"", json);
        Assert.Contains("\"rideId\"", json);
        Assert.Contains("\"reason\"", json);
        Assert.Contains("\"requestedAt\"", json);
    }

    [Fact]
    public void EventContractsJsonOptions_CanRoundtrip_WeatherAlertEvent()
    {
        var original = new WeatherAlertEvent(
            EventId: Guid.NewGuid(),
            Severity: WeatherSeverity.Severe,
            AffectedZones: ["Zone-A", "Zone-C"],
            GeneratedAt: DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(original, EventContractsJsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<WeatherAlertEvent>(json, EventContractsJsonOptions.Default);

        Assert.NotNull(deserialized);
        Assert.Equal(original.EventId, deserialized!.EventId);
        Assert.Equal(original.Severity, deserialized.Severity);
        Assert.Equal(original.AffectedZones, deserialized.AffectedZones);
    }
}
