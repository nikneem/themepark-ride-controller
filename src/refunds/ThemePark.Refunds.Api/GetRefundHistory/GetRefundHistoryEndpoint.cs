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
            return await handler.HandleAsync(rideId, ct);
        })
        .WithName("GetRefundHistory")
        .WithSummary("Get refund history for a ride (last 20 batches)")
        .WithTags("Refunds");

        return app;
    }
}
