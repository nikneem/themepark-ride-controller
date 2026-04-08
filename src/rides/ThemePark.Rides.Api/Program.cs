using ThemePark.Rides;
using ThemePark.Rides.Api.Features.Rides;
using ThemePark.Rides.Api.GetRide;
using ThemePark.Rides.Api.PauseRide;
using ThemePark.Rides.Api.ResumeRide;
using ThemePark.Rides.Api.SimulateMalfunction;
using ThemePark.Rides.Api.Startup;
using ThemePark.Rides.Api.StartRide;
using ThemePark.Rides.Api.StopRide;
using ThemePark.Rides.Data.Dapr;
using ThemePark.Rides.Exceptions;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
// Infrastructure registrations
builder.Services.AddScoped<IRideStateStore, DaprRideStateStore>();
builder.Services.AddScoped<IRideStateRepository, RideStateRepository>();
builder.Services.AddScoped<RideCommandHandlers>();
builder.Services.AddHostedService<RideSeedService>();

// Domain handlers via module registration
builder.Services.AddRidesModule();

var app = builder.Build();

app.MapDefaultEndpoints();

// Vertical slice endpoints
app.MapGetRide();
app.MapStartRide();
app.MapPauseRide();
app.MapResumeRide();
app.MapStopRide();
app.MapSimulateMalfunction();

// Legacy endpoints (kept for backward compatibility with workflow activities)
app.MapGet("/api/rides/{rideId}/status", async (string rideId, IRideStateRepository repo, CancellationToken ct) =>
{
    var status = await repo.GetStatusAsync(rideId, ct);
    return Results.Ok(new { rideId, status = status.ToString() });
});

app.MapGet("/api/rides", async (IRideStateRepository repo, CancellationToken ct) =>
{
    var rides = ThemePark.Shared.Catalog.RideCatalog.All;
    var results = await Task.WhenAll(rides.Select(async r =>
    {
        var status = await repo.GetStatusAsync(r.RideId.ToString(), ct);
        return new { r.RideId, r.Name, r.Capacity, r.Zone, status = status.ToString() };
    }));
    return Results.Ok(results);
});

app.MapPost("/api/rides/{rideId}/transition", async (
    string rideId,
    TransitionRequest request,
    RideCommandHandlers handlers,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<RideStatus>(request.TargetStatus, ignoreCase: true, out var target))
        return Results.BadRequest(new { error = $"Unknown status: {request.TargetStatus}" });

    try
    {
        var newStatus = await handlers.TransitionAsync(rideId, target, ct);
        return Results.Ok(new { rideId, status = newStatus.ToString() });
    }
    catch (InvalidRideTransitionException ex)
    {
        return Results.Conflict(new { error = ex.Message, from = ex.FromStatus.ToString(), to = ex.ToStatus.ToString() });
    }
});

app.Run();

internal sealed record TransitionRequest(string TargetStatus);

