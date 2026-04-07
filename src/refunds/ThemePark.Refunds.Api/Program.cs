using ThemePark.Refunds.Api.GetRefundHistory;
using ThemePark.Refunds.Api.IssueRefund;
using ThemePark.Refunds.Data.Dapr;
using ThemePark.Refunds.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IRefundStateStore, DaprRefundStateStore>();
builder.Services.AddScoped<IssueRefundHandler>();
builder.Services.AddScoped<GetRefundHistoryHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapIssueRefund();
app.MapGetRefundHistory();

app.Run();

