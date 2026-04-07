using ThemePark.Queue.Api.GetQueue;
using ThemePark.Queue.Api.LoadPassengers;
using ThemePark.Queue.Api.SimulateQueue;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Vertical slice handlers
builder.Services.AddScoped<GetQueueHandler>();
builder.Services.AddScoped<LoadPassengersHandler>();
builder.Services.AddScoped<SimulateQueueHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

// Endpoints
app.MapGetQueue();
app.MapLoadPassengers();
app.MapSimulateQueue(app.Configuration);

app.Run();
