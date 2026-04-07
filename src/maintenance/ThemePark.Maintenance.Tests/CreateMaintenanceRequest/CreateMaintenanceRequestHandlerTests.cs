using Dapr.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using ThemePark.Maintenance.Api.CreateMaintenanceRequest;
using ThemePark.Maintenance.Api.State;
using ThemePark.Maintenance.Models;
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

        var created = Assert.IsType<Created<CreateMaintenanceRequestResponse>>(result.Result);
        Assert.Equal(command.RideId, created.Value!.RideId);
        Assert.Equal("Pending", created.Value.Status);
        Assert.NotEqual(Guid.Empty, created.Value.MaintenanceId);
    }

    [Fact]
    public async Task HandleAsync_EmptyRideId_ReturnsBadRequest()
    {
        var command = new CreateMaintenanceRequestCommand(Guid.Empty, "ScheduledCheck", null, default);

        var result = await CreateSut().HandleAsync(command);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task HandleAsync_InvalidReason_ReturnsBadRequest()
    {
        var command = new CreateMaintenanceRequestCommand(Guid.NewGuid(), "FakeReason", null, default);

        var result = await CreateSut().HandleAsync(command);

        Assert.IsType<BadRequest<string>>(result.Result);
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
