using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Refunds.Api.IssueRefund;
using ThemePark.Refunds.State;
using ThemePark.Refunds.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Refunds.Tests.IssueRefund;

public sealed class IssueRefundHandlerTests
{
    private readonly Mock<IRefundStateStore> _store = new();
    private readonly Guid _rideId = Guid.NewGuid();
    private const string WorkflowId = "wf-test-001";

    private IssueRefundHandler CreateHandler() => new(_store.Object);

    private void SetupEmptyHistory()
    {
        _store.Setup(s => s.GetHistoryAsync(_rideId, It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);
    }

    [Fact]
    public async Task StandardPassenger_Returns10PerPassenger_NoVouchers()
    {
        SetupEmptyHistory();
        var request = new IssueRefundRequest(
            _rideId, WorkflowId, "MechanicalFailure",
            [new RefundPassengerRequest("p1", false)]);

        var result = await CreateHandler().HandleAsync(request);

        var ok = Assert.IsType<Ok<IssueRefundResponse>>(result.Result);
        Assert.Equal(1, ok.Value!.TotalRefunded);
        Assert.Equal(10.00m, ok.Value.TotalAmount);
        Assert.Equal(0, ok.Value.VoucherCount);
    }

    [Fact]
    public async Task VipPassenger_Returns10AmountAnd1Voucher()
    {
        SetupEmptyHistory();
        var request = new IssueRefundRequest(
            _rideId, WorkflowId, "MechanicalFailure",
            [new RefundPassengerRequest("vip1", true)]);

        var result = await CreateHandler().HandleAsync(request);

        var ok = Assert.IsType<Ok<IssueRefundResponse>>(result.Result);
        Assert.Equal(1, ok.Value!.TotalRefunded);
        Assert.Equal(10.00m, ok.Value.TotalAmount);
        Assert.Equal(1, ok.Value.VoucherCount);
    }

    [Fact]
    public async Task MixedPassengers_Calculates3RefundsAnd2Vouchers()
    {
        SetupEmptyHistory();
        var request = new IssueRefundRequest(
            _rideId, WorkflowId, "WeatherClosure",
            [
                new RefundPassengerRequest("p1", false),
                new RefundPassengerRequest("vip1", true),
                new RefundPassengerRequest("vip2", true)
            ]);

        var result = await CreateHandler().HandleAsync(request);

        var ok = Assert.IsType<Ok<IssueRefundResponse>>(result.Result);
        Assert.Equal(3, ok.Value!.TotalRefunded);
        Assert.Equal(30.00m, ok.Value.TotalAmount);
        Assert.Equal(2, ok.Value.VoucherCount);
    }

    [Fact]
    public async Task DuplicateWorkflowId_ReturnsExistingBatchUnchanged()
    {
        var existingSummary = new RefundBatchSummary(
            Guid.NewGuid(), WorkflowId, RefundReason.MechanicalFailure,
            1, 10.00m, 0, DateTimeOffset.UtcNow.AddMinutes(-5));

        _store.Setup(s => s.GetHistoryAsync(_rideId, It.IsAny<CancellationToken>()))
              .ReturnsAsync([existingSummary]);

        var request = new IssueRefundRequest(
            _rideId, WorkflowId, "MechanicalFailure",
            [new RefundPassengerRequest("p1", false)]);

        var result = await CreateHandler().HandleAsync(request);

        var ok = Assert.IsType<Ok<IssueRefundResponse>>(result.Result);
        Assert.Equal(existingSummary.RefundBatchId, ok.Value!.RefundBatchId);
        Assert.Equal(WorkflowId, ok.Value.WorkflowId);
        // No new save should have occurred
        _store.Verify(s => s.SaveBatchAsync(It.IsAny<RefundBatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HistoryAt20_NewEntryAdded_OldestDropped()
    {
        var existing = Enumerable.Range(0, 20)
            .Select(i => new RefundBatchSummary(
                Guid.NewGuid(), $"wf-old-{i}", RefundReason.OperationalDecision,
                1, 10.00m, 0, DateTimeOffset.UtcNow.AddMinutes(-i - 1)))
            .ToList();

        _store.Setup(s => s.GetHistoryAsync(_rideId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(existing);

        List<RefundBatchSummary>? savedHistory = null;
        _store.Setup(s => s.SaveHistoryAsync(_rideId, It.IsAny<IReadOnlyList<RefundBatchSummary>>(), It.IsAny<CancellationToken>()))
              .Callback<Guid, IReadOnlyList<RefundBatchSummary>, CancellationToken>((_, h, _) => savedHistory = h.ToList())
              .Returns(Task.CompletedTask);

        var request = new IssueRefundRequest(
            _rideId, WorkflowId, "MechanicalFailure",
            [new RefundPassengerRequest("p1", false)]);

        await CreateHandler().HandleAsync(request);

        Assert.NotNull(savedHistory);
        Assert.Equal(20, savedHistory!.Count);
        Assert.Equal(WorkflowId, savedHistory[0].WorkflowId);
        Assert.DoesNotContain(savedHistory, s => s.WorkflowId == "wf-old-19");
    }

    [Fact]
    public async Task InvalidReason_ReturnsBadRequest()
    {
        SetupEmptyHistory();
        var request = new IssueRefundRequest(
            _rideId, WorkflowId, "NotARealReason",
            [new RefundPassengerRequest("p1", false)]);

        var result = await CreateHandler().HandleAsync(request);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task EmptyRideId_ReturnsBadRequest()
    {
        var request = new IssueRefundRequest(
            Guid.Empty, WorkflowId, "MechanicalFailure",
            [new RefundPassengerRequest("p1", false)]);

        var result = await CreateHandler().HandleAsync(request);

        Assert.IsType<BadRequest<string>>(result.Result);
    }
}
