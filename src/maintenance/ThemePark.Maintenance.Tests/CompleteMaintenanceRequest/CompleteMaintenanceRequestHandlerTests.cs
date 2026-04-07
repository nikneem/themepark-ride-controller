using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Maintenance.Api.CompleteMaintenanceRequest;
using ThemePark.Maintenance.Api.State;
using ThemePark.Maintenance.Models;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Tests.CompleteMaintenanceRequest;

public sealed class CompleteMaintenanceRequestHandlerTests
{
    private readonly Mock<IMaintenanceStateStore> _stateStore = new();
    private readonly Mock<DaprClient> _daprClient = new();

    private Api.CompleteMaintenanceRequest.CompleteMaintenanceRequestHandler CreateSut() =>
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

        var result = await CreateSut().HandleAsync(new Api.CompleteMaintenanceRequest.CompleteMaintenanceRequestCommand(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCompleted_ReturnsConflict()
    {
        var record = ActiveRecord(status: MaintenanceStatus.Completed);
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new Api.CompleteMaintenanceRequest.CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.IsType<Conflict<string>>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCancelled_ReturnsConflict()
    {
        var record = ActiveRecord(status: MaintenanceStatus.Cancelled);
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new Api.CompleteMaintenanceRequest.CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.IsType<Conflict<string>>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_PendingRecord_CompletesAndPublishes()
    {
        var record = ActiveRecord();
        _stateStore.Setup(s => s.GetRecordAsync(record.MaintenanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await CreateSut().HandleAsync(new Api.CompleteMaintenanceRequest.CompleteMaintenanceRequestCommand(record.MaintenanceId));

        var ok = Assert.IsType<Ok<CompleteMaintenanceRequestResponse>>(result.Result);
        Assert.Equal("Completed", ok.Value!.Status);
        Assert.NotNull(ok.Value.DurationMinutes);
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

        var result = await CreateSut().HandleAsync(new Api.CompleteMaintenanceRequest.CompleteMaintenanceRequestCommand(record.MaintenanceId));

        Assert.IsType<Ok<CompleteMaintenanceRequestResponse>>(result.Result);
    }
}
