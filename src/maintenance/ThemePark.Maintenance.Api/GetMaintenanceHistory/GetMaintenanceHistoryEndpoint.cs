namespace ThemePark.Maintenance.Api.GetMaintenanceHistory;

public static class GetMaintenanceHistoryEndpoint
{
    public static IEndpointRouteBuilder MapGetMaintenanceHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/maintenance/history/{rideId:guid}", async (
            Guid rideId,
            GetMaintenanceHistoryHandler handler,
            CancellationToken ct) =>
        {
            return await handler.HandleAsync(rideId, ct);
        })
        .WithName("GetMaintenanceHistory")
        .WithSummary("Get maintenance history for a ride")
        .WithTags("Maintenance");

        return app;
    }
}
