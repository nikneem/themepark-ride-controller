using ThemePark.Maintenance.Api.CompleteMaintenanceRequest;
using ThemePark.Maintenance.Api.CreateMaintenanceRequest;
using ThemePark.Maintenance.Api.GetMaintenanceHistory;
using ThemePark.Maintenance.Data.Dapr;
using ThemePark.Maintenance.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IMaintenanceStateStore, DaprMaintenanceStateStore>();
builder.Services.AddScoped<CreateMaintenanceRequestHandler>();
builder.Services.AddScoped<CompleteMaintenanceRequestHandler>();
builder.Services.AddScoped<GetMaintenanceHistoryHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapCreateMaintenanceRequest();
app.MapCompleteMaintenanceRequest();
app.MapGetMaintenanceHistory();

app.Run();

