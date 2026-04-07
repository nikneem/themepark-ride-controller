using Dapr.Client;
using Dapr.Workflow;
using System.Net.Http.Json;

namespace ThemePark.ControlCenter.Workflow.Activities;

/// <summary>Local DTO for the POST /refunds request body.</summary>
internal sealed record IssueRefundPassenger(string PassengerId, bool IsVip);

/// <summary>Local DTO matching the refund-service POST /refunds request.</summary>
internal sealed record IssueRefundRequest(
    Guid RideId,
    string WorkflowId,
    string Reason,
    IReadOnlyList<IssueRefundPassenger> Passengers);

/// <summary>Local DTO matching the refund-service POST /refunds response.</summary>
internal sealed record IssueRefundServiceResponse(
    Guid RefundBatchId,
    Guid RideId,
    string WorkflowId,
    string Reason,
    int TotalRefunded,
    decimal TotalAmount,
    int VoucherCount,
    DateTimeOffset ProcessedAt);

/// <summary>Input for the IssueRefundActivity.</summary>
public sealed record IssueRefundActivityInput(
    string RideId,
    string WorkflowId,
    string Reason,
    IReadOnlyList<RidePassenger> Passengers);

/// <summary>Output from the IssueRefundActivity.</summary>
public sealed record IssueRefundActivityOutput(
    Guid RefundBatchId,
    int TotalRefunded,
    decimal TotalAmount,
    int VoucherCount);

/// <summary>
/// Dapr Workflow activity that calls POST /refunds on refund-service via Dapr service invocation.
/// Acts as the compensation step in RideWorkflow when a ride session ends in failure.
/// </summary>
public sealed class IssueRefundActivity : WorkflowActivity<IssueRefundActivityInput, IssueRefundActivityOutput>
{
    // CreateInvokeHttpClient routes HTTP requests through the Dapr sidecar to the named app.
    private static readonly HttpClient HttpClient = DaprClient.CreateInvokeHttpClient("refunds-api");

    public override async Task<IssueRefundActivityOutput> RunAsync(
        WorkflowActivityContext context,
        IssueRefundActivityInput input)
    {
        var passengers = input.Passengers
            .Select(p => new IssueRefundPassenger(p.PassengerId, p.IsVip))
            .ToList();

        var request = new IssueRefundRequest(
            Guid.Parse(input.RideId),
            input.WorkflowId,
            input.Reason,
            passengers);

        var httpResponse = await HttpClient.PostAsJsonAsync("/refunds", request);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content
            .ReadFromJsonAsync<IssueRefundServiceResponse>()
            ?? throw new InvalidOperationException("Empty response from refund-service.");

        return new IssueRefundActivityOutput(
            response.RefundBatchId,
            response.TotalRefunded,
            response.TotalAmount,
            response.VoucherCount);
    }
}
