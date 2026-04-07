using System.Text.Json;
using System.Threading.Channels;
using Dapr;
using ThemePark.ControlCenter.Infrastructure;
using ThemePark.ControlCenter.PubSub;
using ThemePark.ControlCenter.Workflow;
using Dapr.Workflow;
using ThemePark.ControlCenter.Workflow.Activities;
using ThemePark.EventContracts.Events;
using ThemePark.EventContracts.Serialization;
using ThemePark.Rides.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Dapr Workflow — registers RideWorkflow and all activities
builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterWorkflow<RideWorkflow>();
    options.RegisterActivity<StartPreFlightActivity>();
    options.RegisterActivity<StartLoadingActivity>();
    options.RegisterActivity<StartRunActivity>();
    options.RegisterActivity<PauseRideActivity>();
    options.RegisterActivity<ResumeRideActivity>();
    options.RegisterActivity<EnterMaintenanceActivity>();
    options.RegisterActivity<StartResumingActivity>();
    options.RegisterActivity<CompleteRideActivity>();
    options.RegisterActivity<FailRideActivity>();
    options.RegisterActivity<ResetRideActivity>();
    options.RegisterActivity<IssueRefundActivity>();
});

// Singleton channel that fans ride.status-changed pub/sub events to SSE clients.
var statusChannel = Channel.CreateUnbounded<RideStatusChangedEvent>(
    new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });
builder.Services.AddSingleton(statusChannel);
builder.Services.AddSingleton(statusChannel.Writer);
builder.Services.AddSingleton(statusChannel.Reader);

// Infrastructure + pub/sub
builder.Services.AddScoped<IRideStateRepository, RideStateRepository>();
builder.Services.AddScoped<IRideStatusEventPublisher, RideStatusEventPublisher>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// Registers the Dapr subscription endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

// SSE endpoint — streams ride.status-changed events to connected frontend clients.
app.MapGet("/api/events/stream", async (
    ChannelReader<RideStatusChangedEvent> reader,
    HttpResponse response,
    CancellationToken ct) =>
{
    response.Headers.ContentType = "text/event-stream";
    response.Headers.CacheControl = "no-cache";
    response.Headers.Connection = "keep-alive";

    await foreach (var evt in reader.ReadAllAsync(ct))
    {
        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);
        await response.WriteAsync($"data: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
});

// Dapr subscribers — one endpoint per event contract.

app.MapPost("/events/ride-status-changed",
    [Topic("themepark-pubsub", "ride.status-changed", DeadLetterTopic = "ride.status-changed.deadletter")]
    async (RideStatusChangedEvent evt, ChannelWriter<RideStatusChangedEvent> writer, ILogger<Program> log) =>
    {
        log.LogInformation("Ride {RideId} status changed: {From} → {To}", evt.RideId, evt.PreviousStatus, evt.NewStatus);
        await writer.WriteAsync(evt);
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
