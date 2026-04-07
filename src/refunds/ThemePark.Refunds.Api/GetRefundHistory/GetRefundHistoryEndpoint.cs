using ThemePark.Refunds.Abstractions.DataTransferObjects;
using ThemePark.Refunds.Features.GetRefundHistory;

namespace ThemePark.Refunds.Api.GetRefundHistory;

public static class GetRefundHistoryEndpoint
{
    public static IEndpointRouteBuilder MapGetRefundHistory(this IEndpointRouteBuilder app)
    {
        app.MapGet("/refunds/{rideId:guid}/history", async (
            Guid rideId,
            GetRefundHistoryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetRefundHistoryQuery(rideId), ct);
            return Results.Ok(result.Value);
        })
        .WithName("GetRefundHistory")
        .WithSummary("Get refund history for a ride (last 20 batches)")
        .WithTags("Refunds");

        return app;
    }
}

