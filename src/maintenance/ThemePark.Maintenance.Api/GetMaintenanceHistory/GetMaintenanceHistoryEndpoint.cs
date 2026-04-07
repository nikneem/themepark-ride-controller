using ThemePark.Maintenance.Features.GetMaintenanceHistory;
using ThemePark.Shared;

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
            var result = await handler.HandleAsync(new GetMaintenanceHistoryQuery(rideId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound();
        })
        .WithName("GetMaintenanceHistory")
        .WithSummary("Get maintenance history for a ride")
        .WithTags("Maintenance");

        return app;
    }
}

