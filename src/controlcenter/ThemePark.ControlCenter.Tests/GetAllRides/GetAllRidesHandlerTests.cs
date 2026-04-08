using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ThemePark.ControlCenter.Features.GetAllRides;

namespace ThemePark.ControlCenter.Tests.GetAllRides;

public sealed class GetAllRidesHandlerTests
{
    // DaprClient.InvokeMethodAsync<T>(HttpMethod, string, string, CancellationToken) calls
    // CreateInvokeMethodRequest internally, which NSubstitute also intercepts, causing
    // arg-spec conflicts. Happy-path coverage is provided by integration tests.

    [Fact]
    public async Task HandleAsync_DaprThrowsGenericException_ReturnsEmptyList()
    {
        // Arrange — make CreateInvokeMethodRequest throw so the outer call never proceeds
        var daprClient = Substitute.For<DaprClient>();
        daprClient
            .CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new Exception("Dapr unavailable"));

        var handler = new GetAllRidesHandler(daprClient, NullLogger<GetAllRidesHandler>.Instance);

        // Act
        var result = await handler.HandleAsync(new GetAllRidesQuery());

        // Assert — handler swallows exceptions and returns empty list
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_QueryCreated_HandlerDoesNotThrow()
    {
        // Smoke test: verifies the handler can be constructed and called without throwing.
        var daprClient = Substitute.For<DaprClient>();
        var handler = new GetAllRidesHandler(daprClient, NullLogger<GetAllRidesHandler>.Instance);

        var exception = await Record.ExceptionAsync(
            () => handler.HandleAsync(new GetAllRidesQuery()));

        Assert.Null(exception);
    }
}
