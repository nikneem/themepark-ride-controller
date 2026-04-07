using ThemePark.Mascots;
using ThemePark.Mascots.Api.ClearMascot;
using ThemePark.Mascots.Api.GetMascots;
using ThemePark.Mascots.Api.Services;
using ThemePark.Mascots.Api.SimulateIntrusion;
using ThemePark.Mascots.Data.InMemory;
using ThemePark.Mascots.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Singleton in-memory state store (no persistence — resets on restart by design)
builder.Services.AddSingleton<IMascotStateStore, InMemoryMascotStateStore>();

// Domain handlers via module registration
builder.Services.AddMascotsModule();

// Background timer that randomly moves mascots between zones
builder.Services.AddHostedService<MascotMovementService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapGetMascots();
app.MapClearMascot();
app.MapSimulateIntrusion(app.Configuration);

app.Run();

