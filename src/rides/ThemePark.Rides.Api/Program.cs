using ThemePark.Rides.Api._Shared;
using ThemePark.Rides.Api.Features.Rides;
using ThemePark.Rides.Api.Infrastructure;
using ThemePark.Rides.Api.Startup;
using ThemePark.Rides.Exceptions;
using ThemePark.Rides.Infrastructure;
using ThemePark.Shared.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
builder.Services.AddScoped<IRideStateStore, DaprRideStateStore>();
builder.Services.AddScoped<IRideStateRepository, RideStateRepository>();
builder.Services.AddScoped<RideCommandHandlers>();
builder.Services.AddHostedService<RideSeedService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// GET /api/rides/{rideId}/status — returns current ride status from Dapr state store
app.MapGet("/api/rides/{rideId}/status", async (string rideId, IRideStateRepository repo, CancellationToken ct) =>
{
    var status = await repo.GetStatusAsync(rideId, ct);
    return Results.Ok(new { rideId, status = status.ToString() });
});

// GET /api/rides — returns all catalog rides with their current status
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

// POST /api/rides/{rideId}/transition — manually trigger a state transition
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
