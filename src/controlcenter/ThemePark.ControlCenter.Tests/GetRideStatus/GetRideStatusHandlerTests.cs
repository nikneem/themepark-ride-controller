using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ThemePark.ControlCenter.Features.GetRideStatus;

namespace ThemePark.ControlCenter.Tests.GetRideStatus;

public sealed class GetRideStatusHandlerTests
{
    // DaprClient.InvokeMethodAsync<T>(HttpMethod, string, string, CancellationToken) calls
    // CreateInvokeMethodRequest internally, which NSubstitute also intercepts and causes
    // arg-spec conflicts. Tests that need happy-path DaprClient invocations are covered
    // by integration tests in ThemePark.IntegrationTests.

    [Fact]
    public async Task HandleAsync_DaprThrowsGenericException_ReturnsNull()
    {
        // Arrange — configure the substitute so that InvokeMethodAsync throws
        // without using Arg.Any<> matchers on the multi-arg overload.
        var daprClient = Substitute.For<DaprClient>();
        daprClient
            .CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>())
            .Throws(new Exception("Dapr unavailable"));

        var handler = new GetRideStatusHandler(daprClient, NullLogger<GetRideStatusHandler>.Instance);

        // Act
        var result = await handler.HandleAsync(new GetRideStatusQuery("any-ride-id"));

        // Assert — handler swallows exceptions and returns null
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_QueryCreated_HandlerDoesNotThrow()
    {
        // Smoke test: verifies the handler can be constructed and called without throwing,
        // even when DaprClient has no setup (returns default / null).
        var daprClient = Substitute.For<DaprClient>();
        var handler = new GetRideStatusHandler(daprClient, NullLogger<GetRideStatusHandler>.Instance);

        var exception = await Record.ExceptionAsync(
            () => handler.HandleAsync(new GetRideStatusQuery(Guid.NewGuid().ToString())));

        Assert.Null(exception);
    }
}
