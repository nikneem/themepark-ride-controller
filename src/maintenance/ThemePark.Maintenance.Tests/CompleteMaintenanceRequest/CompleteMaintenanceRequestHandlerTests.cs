using Dapr.Client;
using Moq;
using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Features.CompleteMaintenanceRequest;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Tests.CompleteMaintenanceRequest;

public sealed class CompleteMaintenanceRequestHandlerTests
{
    private readonly Mock<IMaintenanceStateStore> _stateStore = new();
    private readonly Mock<DaprClient> _daprClient = new();

    private CompleteMaintenanceRequestHandler CreateSut() =>
        new(_stateStore.Object, _daprClient.Object);

    private MaintenanceRecord ActiveRecord(Guid? id = null, MaintenanceStatus status = MaintenanceStatus.Pending)
    {
        var maintenanceId = id ?? Guid.NewGuid();
        return new MaintenanceRecord(maintenanceId, Guid.NewGuid(), MaintenanceReason.MechanicalFailure,
            status, null, DateTimeOffset.UtcNow.AddMinutes(-15), null);
    }

    [Fact]
    public async Task HandleAsync_RecordNotFound_ReturnsNotFound()
    {
        _stateStore.Setup(s => s.GetRecordAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaintenanceRecord?)null);

        var result = await CreateSut().HandleAsync(new CompleteMaintenanceRequestCommand(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCompleted_ReturnsConflict()
    {
        var record = ActiveRecord(status: MaintenanceStatus.Completed);
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCancelled_ReturnsConflict()
    {
        var record = ActiveRecord(status: MaintenanceStatus.Cancelled);
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.Conflict, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_PendingRecord_CompletesAndPublishes()
    {
        var record = ActiveRecord();
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value!.Status);
        Assert.NotNull(result.Value.DurationMinutes);
        _stateStore.Verify(s => s.SaveRecordAsync(It.IsAny<MaintenanceRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _daprClient.Verify(d => d.PublishEventAsync(
            "themepark-pubsub", "maintenance.completed",
            It.IsAny<ThemePark.EventContracts.Events.MaintenanceCompletedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InProgressRecord_Completes()
    {
        var record = ActiveRecord(status: MaintenanceStatus.InProgress);
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.True(result.IsSuccess);
    }
}

