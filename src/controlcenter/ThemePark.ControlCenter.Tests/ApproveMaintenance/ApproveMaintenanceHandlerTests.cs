using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThemePark.ControlCenter.Features.ApproveMaintenance;

namespace ThemePark.ControlCenter.Tests.ApproveMaintenance;

public sealed class ApproveMaintenanceHandlerTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();

    private ApproveMaintenanceHandler CreateHandler() =>
        new(_daprClient, null!, NullLogger<ApproveMaintenanceHandler>.Instance);

    [Fact]
    public async Task HandleAsync_NoActiveWorkflow_ReturnsFalse()
    {
        _daprClient
            .GetStateAsync<string?>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await CreateHandler().HandleAsync(new ApproveMaintenanceCommand("ride-001"));

        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_EmptyInstanceId_ReturnsFalse()
    {
        _daprClient
            .GetStateAsync<string?>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var result = await CreateHandler().HandleAsync(new ApproveMaintenanceCommand("ride-001"));

        Assert.False(result);
    }
}
