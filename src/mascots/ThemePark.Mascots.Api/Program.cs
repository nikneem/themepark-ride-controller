using ThemePark.Mascots.Api.ClearMascot;
using ThemePark.Mascots.Api.GetMascots;
using ThemePark.Mascots.Api.Services;
using ThemePark.Mascots.Api.SimulateIntrusion;
using ThemePark.Mascots.Api.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Singleton in-memory state store (no persistence — resets on restart by design)
builder.Services.AddSingleton<MascotStateStore>();

// Vertical slice handlers
builder.Services.AddScoped<GetMascotsHandler>();
builder.Services.AddScoped<ClearMascotHandler>();
builder.Services.AddScoped<SimulateIntrusionHandler>();

// Background timer that randomly moves mascots between zones
builder.Services.AddHostedService<MascotMovementService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapGetMascots();
app.MapClearMascot();
app.MapSimulateIntrusion(app.Configuration);

app.Run();
