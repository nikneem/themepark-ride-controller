using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThemePark.ControlCenter.Features.ResolveChaosEvent;
using ThemePark.Aspire.ServiceDefaults;

namespace ThemePark.ControlCenter.Tests.ResolveChaosEvent;

/// <remarks>
/// <c>DaprWorkflowClient</c> is a sealed class in the Dapr.Workflow SDK and cannot be
/// substituted. Tests that require the success path (raise-event) are covered by the
/// integration tests in <c>ThemePark.IntegrationTests</c>.
/// The tests here cover the no-active-workflow branch that does not reach
/// <c>DaprWorkflowClient.RaiseEventAsync</c>.
/// </remarks>
public sealed class ResolveChaosEventHandlerTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();

    [Fact]
    public async Task HandleAsync_NoActiveWorkflow_ReturnsFalse()
    {
        _daprClient
            .GetStateAsync<string?>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // DaprWorkflowClient is sealed; pass null! — it is never reached in this code path.
        var handler = new ResolveChaosEventHandler(_daprClient, null!, NullLogger<ResolveChaosEventHandler>.Instance);
        var command = new ResolveChaosEventCommand("ride-001", Guid.NewGuid().ToString(), "WeatherAlert");

        var result = await handler.HandleAsync(command);

        Assert.False(result);
        await _daprClient.Received(1).GetStateAsync<string?>(
            Arg.Any<string>(),
            $"active-workflow-{command.RideId}",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyInstanceId_ReturnsFalse()
    {
        _daprClient
            .GetStateAsync<string?>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var handler = new ResolveChaosEventHandler(_daprClient, null!, NullLogger<ResolveChaosEventHandler>.Instance);
        var command = new ResolveChaosEventCommand("ride-001", Guid.NewGuid().ToString(), "MascotIntrusion");

        var result = await handler.HandleAsync(command);

        Assert.False(result);
    }
}
