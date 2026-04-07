using ThemePark.Refunds.Abstractions.DataTransferObjects;
using ThemePark.Refunds.Features.IssueRefund;
using ThemePark.Shared;

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
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ErrorKind == OperationErrorKind.BadRequest
                    ? Results.BadRequest(new { error = result.Error })
                    : Results.Conflict(new { error = result.Error });
        })
        .WithName("IssueRefund")
        .WithSummary("Issue a batch refund for a ride failure")
        .WithTags("Refunds");

        return app;
    }
}

