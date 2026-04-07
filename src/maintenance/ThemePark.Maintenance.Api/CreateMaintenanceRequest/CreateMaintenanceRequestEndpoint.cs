using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Features.CreateMaintenanceRequest;
using ThemePark.Shared;

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
            var result = await handler.HandleAsync(command, ct);
            return result.IsSuccess
                ? Results.Created($"/maintenance/{result.Value!.MaintenanceId}", result.Value)
                : result.ErrorKind == OperationErrorKind.BadRequest
                    ? Results.BadRequest(new { error = result.Error })
                    : Results.StatusCode(500);
        })
        .WithName("CreateMaintenanceRequest")
        .WithSummary("Create a new maintenance request for a ride")
        .WithTags("Maintenance");

        return app;
    }
}

