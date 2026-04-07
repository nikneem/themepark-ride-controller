namespace ThemePark.Maintenance.Api.CompleteMaintenanceRequest;

public static class CompleteMaintenanceRequestEndpoint
{
    public static IEndpointRouteBuilder MapCompleteMaintenanceRequest(this IEndpointRouteBuilder app)
    {
        app.MapPut("/maintenance/{maintenanceId:guid}/complete", async (
            Guid maintenanceId,
            CompleteMaintenanceRequestHandler handler,
            CancellationToken ct) =>
        {
            return await handler.HandleAsync(new CompleteMaintenanceRequestCommand(maintenanceId), ct);
        })
        .WithName("CompleteMaintenanceRequest")
        .WithSummary("Mark a maintenance request as completed")
        .WithTags("Maintenance");

        return app;
    }
}
