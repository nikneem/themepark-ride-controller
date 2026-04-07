using ThemePark.Maintenance.Api.CreateMaintenanceRequest;

namespace ThemePark.Maintenance.Api.CreateMaintenanceRequest;

public static class CreateMaintenanceRequestEndpoint
{
    public static IEndpointRouteBuilder MapCreateMaintenanceRequest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/maintenance", async (
            CreateMaintenanceRequestCommand command,
            CreateMaintenanceRequestHandler handler,
            CancellationToken ct) =>
        {
            return await handler.HandleAsync(command, ct);
        })
        .WithName("CreateMaintenanceRequest")
        .WithSummary("Create a new maintenance request for a ride")
        .WithTags("Maintenance");

        return app;
    }
}
