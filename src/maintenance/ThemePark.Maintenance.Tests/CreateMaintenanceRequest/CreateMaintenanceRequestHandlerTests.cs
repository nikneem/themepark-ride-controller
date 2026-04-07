using Dapr.Client;
using Moq;
using ThemePark.Maintenance.Abstractions.DataTransferObjects;
using ThemePark.Maintenance.Features.CreateMaintenanceRequest;
using ThemePark.Maintenance.Models;
using ThemePark.Maintenance.State;
using ThemePark.Shared;
using ThemePark.Shared.Enums;

namespace ThemePark.Maintenance.Tests.CreateMaintenanceRequest;

public sealed class CreateMaintenanceRequestHandlerTests
{
    private readonly Mock<IMaintenanceStateStore> _stateStore = new();
    private readonly Mock<DaprClient> _daprClient = new();

    private CreateMaintenanceRequestHandler CreateSut() =>
        new(_stateStore.Object, _daprClient.Object);

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsCreated()
    {
        var command = new CreateMaintenanceRequestCommand(
            Guid.NewGuid(), "MechanicalFailure", null, default);

        var result = await CreateSut().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(command.RideId, result.Value!.RideId);
        Assert.Equal("Pending", result.Value.Status);
        Assert.NotEqual(Guid.Empty, result.Value.MaintenanceId);
    }

    [Fact]
    public async Task HandleAsync_EmptyRideId_ReturnsBadRequest()
    {
        var command = new CreateMaintenanceRequestCommand(Guid.Empty, "ScheduledCheck", null, default);

        var result = await CreateSut().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_InvalidReason_ReturnsBadRequest()
    {
        var command = new CreateMaintenanceRequestCommand(Guid.NewGuid(), "FakeReason", null, default);

        var result = await CreateSut().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(OperationErrorKind.BadRequest, result.ErrorKind);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SavesRecordAndHistory()
    {
        var command = new CreateMaintenanceRequestCommand(Guid.NewGuid(), "Failure", null, default);

        await CreateSut().HandleAsync(command);

        _stateStore.Verify(s => s.SaveRecordAsync(It.IsAny<MaintenanceRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _stateStore.Verify(s => s.AppendToRideHistoryAsync(command.RideId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PublishesEvent()
    {
        var command = new CreateMaintenanceRequestCommand(Guid.NewGuid(), "ScheduledCheck", "wf-1", default);

        await CreateSut().HandleAsync(command);

        _daprClient.Verify(d => d.PublishEventAsync(
            "themepark-pubsub", "maintenance.requested",
            It.IsAny<ThemePark.EventContracts.Events.MaintenanceRequestedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DefaultRequestedAt_UsesUtcNow()
    {
        MaintenanceRecord? saved = null;
        _stateStore.Setup(s => s.SaveRecordAsync(It.IsAny<MaintenanceRecord>(), It.IsAny<CancellationToken>()))
            .Callback<MaintenanceRecord, CancellationToken>((r, _) => saved = r)
            .Returns(Task.CompletedTask);

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var command = new CreateMaintenanceRequestCommand(Guid.NewGuid(), "MechanicalFailure", null, default);
        await CreateSut().HandleAsync(command);

        Assert.NotNull(saved);
        Assert.True(saved!.RequestedAt >= before);
    }
}

