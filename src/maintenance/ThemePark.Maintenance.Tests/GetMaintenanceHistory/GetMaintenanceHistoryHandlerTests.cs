using Moq;
using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Features.GetMaintenanceHistory;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Tests.GetMaintenanceHistory;

public sealed class GetMaintenanceHistoryHandlerTests
{
    private readonly Mock<IMaintenanceStateStore> _stateStore = new();

    private GetMaintenanceHistoryHandler CreateSut() => new(_stateStore.Object);

    private static MaintenanceRecord MakeRecord(Guid rideId) =>
        new(Guid.NewGuid(), rideId, MaintenanceReason.ScheduledCheck,
            MaintenanceStatus.Completed, null,
            DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

    [Fact]
    public async Task HandleAsync_NoHistory_ReturnsNotFound()
    {
        _stateStore.Setup(s => s.GetRideHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var result = await CreateSut().HandleAsync(new GetMaintenanceHistoryQuery(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsOkWithItems()
    {
        var rideId = Guid.NewGuid();
        var record = MakeRecord(rideId);
        _stateStore.Setup(s => s.GetRideHistoryAsync(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { record.MaintenanceId });
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new GetMaintenanceHistoryQuery(rideId));

        Assert.True(result.IsSuccess);
        Assert.Equal(rideId, result.Value!.RideId);
        Assert.Single(result.Value.History);
        Assert.Equal(record.MaintenanceId, result.Value.History[0].MaintenanceId);
    }

    [Fact]
    public async Task HandleAsync_MissingRecord_SkipsNullItems()
    {
        var rideId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var record = MakeRecord(rideId);
        _stateStore.Setup(s => s.GetRideHistoryAsync(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { id1, id2 });
        _stateStore.Setup(s => s.GetRecordAsync(id1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaintenanceRecord?)null);
        _stateStore.Setup(s => s.GetRecordAsync(id2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new GetMaintenanceHistoryQuery(rideId));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.History);
    }
}

