using ThemePark.Queue;
using ThemePark.Queue.Api.GetQueue;
using ThemePark.Queue.Api.LoadPassengers;
using ThemePark.Queue.Api.SimulateQueue;
using ThemePark.Queue.Data.Dapr;
using ThemePark.Queue.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
builder.Services.AddScoped<IQueueStateStore, DaprQueueStateStore>();
builder.Services.AddQueueModule();

var app = builder.Build();

app.MapDefaultEndpoints();

// Endpoints
app.MapGetQueue();
app.MapLoadPassengers();
app.MapSimulateQueue(app.Configuration);

app.Run();

