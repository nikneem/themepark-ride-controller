using Dapr;
using ThemePark.EventContracts.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// Registers the Dapr subscription endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

// Subscribers — one endpoint per event contract.
// Implementations will be fleshed out in the control-center-api change.

app.MapPost("/events/ride-status-changed",
    [Topic("themepark-pubsub", "ride.status-changed", DeadLetterTopic = "ride.status-changed.deadletter")]
    (RideStatusChangedEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Ride {RideId} status changed: {From} → {To}", evt.RideId, evt.PreviousStatus, evt.NewStatus);
        return Results.Ok();
    });

app.MapPost("/events/weather-alert",
    [Topic("themepark-pubsub", "weather.alert", DeadLetterTopic = "weather.alert.deadletter")]
    (WeatherAlertEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Weather alert received: {Severity} in zones {Zones}", evt.Severity, string.Join(", ", evt.AffectedZones));
        return Results.Ok();
    });

app.MapPost("/events/mascot-in-restricted-zone",
    [Topic("themepark-pubsub", "mascot.in-restricted-zone", DeadLetterTopic = "mascot.in-restricted-zone.deadletter")]
    (MascotInRestrictedZoneEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Mascot {MascotName} ({MascotId}) in restricted zone of ride {RideId}", evt.MascotName, evt.MascotId, evt.AffectedRideId);
        return Results.Ok();
    });

app.MapPost("/events/ride-malfunction",
    [Topic("themepark-pubsub", "ride.malfunction", DeadLetterTopic = "ride.malfunction.deadletter")]
    (RideMalfunctionEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Ride malfunction on {RideId}: [{FaultCode}] {Description}", evt.RideId, evt.FaultCode, evt.Description);
        return Results.Ok();
    });

app.MapPost("/events/maintenance-requested",
    [Topic("themepark-pubsub", "maintenance.requested", DeadLetterTopic = "maintenance.requested.deadletter")]
    (MaintenanceRequestedEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Maintenance requested for ride {RideId}: {MaintenanceId} — {Reason}", evt.RideId, evt.MaintenanceId, evt.Reason);
        return Results.Ok();
    });

app.MapPost("/events/maintenance-completed",
    [Topic("themepark-pubsub", "maintenance.completed", DeadLetterTopic = "maintenance.completed.deadletter")]
    (MaintenanceCompletedEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Maintenance {MaintenanceId} completed for ride {RideId}", evt.MaintenanceId, evt.RideId);
        return Results.Ok();
    });

app.Run();
