using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Dapr;
using Dapr.Client;
using ThemePark.ControlCenter.Features;
using ThemePark.ControlCenter.Features.ApproveMaintenance;
using ThemePark.ControlCenter.Features.GetAllRides;
using ThemePark.ControlCenter.Features.GetRideHistory;
using ThemePark.ControlCenter.Features.GetRideStatus;
using ThemePark.ControlCenter.Features.ResolveChaosEvent;
using ThemePark.ControlCenter.Features.StartWorkflow;
using ThemePark.ControlCenter.Infrastructure;
using ThemePark.ControlCenter.PubSub;
using ThemePark.ControlCenter.Workflow;
using Dapr.Workflow;
using ThemePark.ControlCenter.Workflow.Activities;
using ThemePark.EventContracts.Events;
using ThemePark.EventContracts.Serialization;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Align HTTP JSON serialization with EventContracts: camelCase properties + string enum values.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

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
    options.RegisterActivity<CheckWeatherActivity>();
    options.RegisterActivity<CheckMascotZoneActivity>();
    options.RegisterActivity<CheckMaintenanceStatusActivity>();
    options.RegisterActivity<CheckSafetySystemsActivity>();
    options.RegisterActivity<StartRideActivity>();
    options.RegisterActivity<StopRideActivity>();
    options.RegisterActivity<CleanupWorkflowActivity>();
    options.RegisterActivity<RecordSessionSummaryActivity>();
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

// Feature handlers
builder.Services.AddScoped<GetAllRidesHandler>();
builder.Services.AddScoped<GetRideStatusHandler>();
builder.Services.AddScoped<StartWorkflowHandler>();
builder.Services.AddScoped<ApproveMaintenanceHandler>();
builder.Services.AddScoped<ResolveChaosEventHandler>();
builder.Services.AddScoped<GetRideHistoryHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// Registers the Dapr subscription endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

// ── Ride management endpoints ────────────────────────────────────────────────

app.MapGet("/api/rides", async (GetAllRidesHandler handler, CancellationToken ct) =>
{
    var rides = await handler.HandleAsync(new GetAllRidesQuery(), ct);
    return Results.Ok(rides);
})
.WithName("GetAllRides")
.WithTags("Rides")
.Produces<IReadOnlyList<RideDto>>();

app.MapGet("/api/rides/{rideId}/status", async (string rideId, GetRideStatusHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(new GetRideStatusQuery(rideId), ct);
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetRideStatus")
.WithTags("Rides")
.Produces<RideStatusResponse>()
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/rides/{rideId}/start", async (string rideId, StartWorkflowHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(new StartWorkflowCommand(rideId, []), ct);
    if (!result.IsSuccess)
    {
        return result.ErrorKind switch
        {
            OperationErrorKind.Conflict  => Results.Conflict(result.Error),
            OperationErrorKind.NotFound  => Results.NotFound(result.Error),
            OperationErrorKind.BadRequest => Results.BadRequest(result.Error),
            _ => Results.Problem(result.Error)
        };
    }
    return Results.Accepted($"/api/rides/{rideId}/status", new StartRideResponse(result.Value!.WorkflowId));
})
.WithName("StartRide")
.WithTags("Rides")
.Produces<StartRideResponse>(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status409Conflict);

app.MapPost("/api/rides/{rideId}/maintenance/approve", async (string rideId, ApproveMaintenanceHandler handler, CancellationToken ct) =>
{
    var approved = await handler.HandleAsync(new ApproveMaintenanceCommand(rideId), ct);
    return approved ? Results.Accepted() : Results.NotFound();
})
.WithName("ApproveMaintenance")
.WithTags("Rides")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/rides/{rideId}/events/{eventId}/resolve", async (
    string rideId,
    string eventId,
    string eventType,
    ResolveChaosEventHandler handler,
    CancellationToken ct) =>
{
    var resolved = await handler.HandleAsync(new ResolveChaosEventCommand(rideId, eventId, eventType), ct);
    return resolved ? Results.Accepted() : Results.NotFound();
})
.WithName("ResolveChaosEvent")
.WithTags("Rides")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/rides/{rideId}/history", async (string rideId, GetRideHistoryHandler handler, CancellationToken ct) =>
{
    var history = await handler.HandleAsync(new GetRideHistoryQuery(rideId), ct);
    return Results.Ok(history);
})
.WithName("GetRideHistory")
.WithTags("Rides")
.Produces<IReadOnlyList<RideHistoryEntry>>();

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

// ── Dapr subscribers — one endpoint per event contract ──────────────────────

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
        // TODO: WeatherAlertEvent has no direct rideId — it targets zones (evt.AffectedZones).
        // To raise WeatherAlertReceived on all affected ride workflows we would need to query
        // all active-workflow-* keys from the state store, which is not directly supported by
        // Dapr's key-value API. A future implementation should maintain a zone→rideId index
        // in the state store so this subscriber can fan-out to each affected workflow.
        log.LogWarning(
            "Weather alert ({Severity}) received for zones [{Zones}] — cannot target a specific workflow without zone→ride mapping. Acknowledging without action.",
            evt.Severity, string.Join(", ", evt.AffectedZones));
        return Results.Ok();
    });

app.MapPost("/events/mascot-in-restricted-zone",
    [Topic("themepark-pubsub", "mascot.in-restricted-zone", DeadLetterTopic = "mascot.in-restricted-zone.deadletter")]
    async (MascotInRestrictedZoneEvent evt, DaprClient daprClient, DaprWorkflowClient workflowClient, ILogger<Program> log) =>
    {
        var rideId = evt.AffectedRideId.ToString();
        var instanceId = await daprClient.GetStateAsync<string?>("themepark-statestore", $"active-workflow-{rideId}");

        if (string.IsNullOrEmpty(instanceId))
        {
            log.LogWarning("Mascot {MascotName} in restricted zone of ride {RideId} — no active workflow, acknowledging.", evt.MascotName, rideId);
            return Results.Ok();
        }

        await workflowClient.RaiseEventAsync(instanceId, "MascotIntrusionReceived", evt);
        log.LogInformation("Raised MascotIntrusionReceived on workflow {InstanceId} for ride {RideId}.", instanceId, rideId);
        return Results.Ok();
    });

app.MapPost("/events/ride-malfunction",
    [Topic("themepark-pubsub", "ride.malfunction", DeadLetterTopic = "ride.malfunction.deadletter")]
    async (RideMalfunctionEvent evt, DaprClient daprClient, DaprWorkflowClient workflowClient, ILogger<Program> log) =>
    {
        var rideId = evt.RideId.ToString();
        var instanceId = await daprClient.GetStateAsync<string?>("themepark-statestore", $"active-workflow-{rideId}");

        if (string.IsNullOrEmpty(instanceId))
        {
            log.LogWarning("Ride malfunction on {RideId} [{FaultCode}] — no active workflow, acknowledging.", rideId, evt.FaultCode);
            return Results.Ok();
        }

        await workflowClient.RaiseEventAsync(instanceId, "MalfunctionReceived", evt);
        log.LogInformation("Raised MalfunctionReceived on workflow {InstanceId} for ride {RideId}.", instanceId, rideId);
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
    async (MaintenanceCompletedEvent evt, DaprClient daprClient, DaprWorkflowClient workflowClient, ILogger<Program> log) =>
    {
        var rideId = evt.RideId.ToString();
        var instanceId = await daprClient.GetStateAsync<string?>("themepark-statestore", $"active-workflow-{rideId}");

        if (string.IsNullOrEmpty(instanceId))
        {
            log.LogWarning("Maintenance {MaintenanceId} completed for ride {RideId} — no active workflow, acknowledging.", evt.MaintenanceId, rideId);
            return Results.Ok();
        }

        await workflowClient.RaiseEventAsync(instanceId, "MaintenanceCompleted", evt);
        log.LogInformation("Raised MaintenanceCompleted on workflow {InstanceId} for ride {RideId}.", instanceId, rideId);
        return Results.Ok();
    });

app.Run();

// Make the implicit Program class visible for WebApplicationFactory-based integration tests.
public partial class Program { }
