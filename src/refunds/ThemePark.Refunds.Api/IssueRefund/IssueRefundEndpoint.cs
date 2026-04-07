namespace ThemePark.Refunds.Api.IssueRefund;

public static class IssueRefundEndpoint
{
    public static IEndpointRouteBuilder MapIssueRefund(this IEndpointRouteBuilder app)
    {
        app.MapPost("/refunds", async (
            IssueRefundRequest request,
            IssueRefundHandler handler,
            CancellationToken ct) =>
        {
            return await handler.HandleAsync(request, ct);
        })
        .WithName("IssueRefund")
        .WithSummary("Issue a batch refund for a ride failure")
        .WithTags("Refunds");

        return app;
    }
}
