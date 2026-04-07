using ThemePark.Maintenance;
using ThemePark.Maintenance.Api.CompleteMaintenanceRequest;
using ThemePark.Maintenance.Api.CreateMaintenanceRequest;
using ThemePark.Maintenance.Api.GetMaintenanceHistory;
using ThemePark.Maintenance.Data.Dapr;
using ThemePark.Maintenance.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IMaintenanceStateStore, DaprMaintenanceStateStore>();
builder.Services.AddMaintenanceModule();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapCreateMaintenanceRequest();
app.MapCompleteMaintenanceRequest();
app.MapGetMaintenanceHistory();

app.Run();


