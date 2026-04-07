using Moq;
using ThemePark.Refunds.Abstractions.DataTransferObjects;
using ThemePark.Refunds.Features.GetRefundHistory;
using ThemePark.Refunds.Models;
using ThemePark.Refunds.State;
using ThemePark.Shared.Enums;

namespace ThemePark.Refunds.Tests.GetRefundHistory;

public sealed class GetRefundHistoryHandlerTests
{
    private readonly Mock<IRefundStateStore> _store = new();
    private readonly Guid _rideId = Guid.NewGuid();

    private GetRefundHistoryHandler CreateHandler() => new(_store.Object);

    [Fact]
    public async Task NoHistory_ReturnsEmptyList()
    {
        _store.Setup(s => s.GetHistoryAsync(_rideId, It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var result = await CreateHandler().HandleAsync(_rideId);

        Assert.True(result.IsSuccess);
        Assert.Equal(_rideId, result.Value!.RideId);
        Assert.Empty(result.Value.History);
    }

    [Fact]
    public async Task ExistingHistory_ReturnsListAsStored()
    {
        var summaries = new List<RefundBatchSummary>
        {
            new(Guid.NewGuid(), "wf-001", RefundReason.MechanicalFailure, 3, 30.00m, 1, DateTimeOffset.UtcNow.AddHours(-1)),
            new(Guid.NewGuid(), "wf-002", RefundReason.WeatherClosure, 2, 20.00m, 0, DateTimeOffset.UtcNow.AddHours(-2))
        };

        _store.Setup(s => s.GetHistoryAsync(_rideId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(summaries);

        var result = await CreateHandler().HandleAsync(_rideId);

        Assert.True(result.IsSuccess);
        Assert.Equal(_rideId, result.Value!.RideId);
        Assert.Equal(2, result.Value.History.Count);
        Assert.Equal("wf-001", result.Value.History[0].WorkflowId);
        Assert.Equal("wf-002", result.Value.History[1].WorkflowId);
    }
}

