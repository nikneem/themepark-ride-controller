using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Features.CompleteMaintenanceRequest;
using ThemePark.Shared;

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
            var result = await handler.HandleAsync(new CompleteMaintenanceRequestCommand(maintenanceId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ErrorKind == OperationErrorKind.NotFound
                    ? Results.NotFound()
                    : Results.Conflict(new { error = result.Error });
        })
        .WithName("CompleteMaintenanceRequest")
        .WithSummary("Mark a maintenance request as completed")
        .WithTags("Maintenance");

        return app;
    }
}

