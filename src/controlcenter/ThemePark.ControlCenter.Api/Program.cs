using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr;
using Dapr.Client;
using ThemePark.Aspire.ServiceDefaults;
using ThemePark.ControlCenter.Domain;
using ThemePark.ControlCenter.Features;
using ThemePark.ControlCenter.Features.ApproveMaintenance;
using ThemePark.ControlCenter.Features.GetAllRides;
using ThemePark.ControlCenter.Features.GetRideHistory;
using ThemePark.ControlCenter.Features.GetRideStatus;
using ThemePark.ControlCenter.Features.ResolveChaosEvent;
using ThemePark.ControlCenter.Features.StartWorkflow;
using ThemePark.ControlCenter.Infrastructure;
using ThemePark.ControlCenter.PubSub;
using ThemePark.ControlCenter.Sse;
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

// Per-connection SSE channel manager — each connected client gets its own channel.
builder.Services.AddSingleton<SseConnectionManager>();

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

// SSE endpoint — streams ride status events to connected frontend clients.
// Each connection gets its own channel so a slow client cannot block others.
// A 15-second heartbeat keeps idle connections alive through proxies.
app.MapGet("/api/events/stream", async (
    SseConnectionManager sseManager,
    HttpResponse response,
    CancellationToken ct) =>
{
    response.Headers.ContentType = "text/event-stream";
    response.Headers.CacheControl = "no-cache";
    response.Headers.Connection = "keep-alive";

    var (connectionId, reader) = sseManager.AddConnection();
    try
    {
        // Flush headers immediately so the client sees the connection is open.
        await response.WriteAsync(": connected\n\n", ct);
        await response.Body.FlushAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            heartbeatCts.CancelAfter(TimeSpan.FromSeconds(15));

            try
            {
                var evt = await reader.ReadAsync(heartbeatCts.Token);
                await response.WriteAsync($"data: {evt.Data}\n\n", ct);
                await response.Body.FlushAsync(ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Heartbeat timeout — send SSE comment to keep connection alive.
                await response.WriteAsync(": heartbeat\n\n", ct);
                await response.Body.FlushAsync(ct);
            }
        }
    }
    finally
    {
        sseManager.RemoveConnection(connectionId);
    }
});

// ── Dapr subscribers — one endpoint per event contract ──────────────────────

app.MapPost("/events/ride-status-changed",
    [Topic(AspireConstants.DaprComponents.PubSub, "ride.status-changed", DeadLetterTopic = "ride.status-changed.deadletter")]
    (RideStatusChangedEvent evt, SseConnectionManager sseManager, ILogger<Program> log) =>
    {
        log.LogInformation("Ride {RideId} status changed: {From} → {To}", evt.RideId, evt.PreviousStatus, evt.NewStatus);
        var json = JsonSerializer.Serialize(evt, EventContractsJsonOptions.Default);
        sseManager.BroadcastEvent(new SseEvent("ride-status-changed", json, DateTimeOffset.UtcNow));
        return Results.Ok();
    });

app.MapPost("/events/weather-alert",
    [Topic(AspireConstants.DaprComponents.PubSub, "weather.alert", DeadLetterTopic = "weather.alert.deadletter")]
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
    [Topic(AspireConstants.DaprComponents.PubSub, "mascot.in-restricted-zone", DeadLetterTopic = "mascot.in-restricted-zone.deadletter")]
    async (MascotInRestrictedZoneEvent evt, DaprClient daprClient, DaprWorkflowClient workflowClient, ILogger<Program> log) =>
    {
        var rideId = evt.AffectedRideId.ToString();
        var instanceId = await daprClient.GetStateAsync<string?>(AspireConstants.DaprComponents.StateStore, $"active-workflow-{rideId}");

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
    [Topic(AspireConstants.DaprComponents.PubSub, "ride.malfunction", DeadLetterTopic = "ride.malfunction.deadletter")]
    async (RideMalfunctionEvent evt, DaprClient daprClient, DaprWorkflowClient workflowClient, ILogger<Program> log) =>
    {
        var rideId = evt.RideId.ToString();
        var instanceId = await daprClient.GetStateAsync<string?>(AspireConstants.DaprComponents.StateStore, $"active-workflow-{rideId}");

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
    [Topic(AspireConstants.DaprComponents.PubSub, "maintenance.requested", DeadLetterTopic = "maintenance.requested.deadletter")]
    (MaintenanceRequestedEvent evt, ILogger<Program> log) =>
    {
        log.LogInformation("Maintenance requested for ride {RideId}: {MaintenanceId} — {Reason}", evt.RideId, evt.MaintenanceId, evt.Reason);
        return Results.Ok();
    });

app.MapPost("/events/maintenance-completed",
    [Topic(AspireConstants.DaprComponents.PubSub, "maintenance.completed", DeadLetterTopic = "maintenance.completed.deadletter")]
    async (MaintenanceCompletedEvent evt, DaprClient daprClient, DaprWorkflowClient workflowClient, ILogger<Program> log) =>
    {
        var rideId = evt.RideId.ToString();
        var instanceId = await daprClient.GetStateAsync<string?>(AspireConstants.DaprComponents.StateStore, $"active-workflow-{rideId}");

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
