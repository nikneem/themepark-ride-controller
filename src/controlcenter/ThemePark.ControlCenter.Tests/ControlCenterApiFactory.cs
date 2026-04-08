using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace ThemePark.ControlCenter.Tests;

/// <summary>
/// WebApplicationFactory for integration tests against the Control Center API.
/// Removes Dapr workflow hosted services that require a live Dapr sidecar during startup,
/// and replaces DaprClient with a no-op substitute so subscriber endpoints that look up
/// active workflow instance IDs don't fail when no sidecar is available.
/// </summary>
public sealed class ControlCenterApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove IHostedService registrations that require the Dapr sidecar or DurableTask
            // runtime to be available. These would cause test host startup to fail when no
            // sidecar is running.
            var daprHostedServices = services
                .Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType is not null &&
                    (d.ImplementationType.FullName!.Contains("Dapr", StringComparison.OrdinalIgnoreCase) ||
                     d.ImplementationType.FullName!.Contains("DurableTask", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var descriptor in daprHostedServices)
                services.Remove(descriptor);

            // Replace DaprClient with a stub that returns null for state-store lookups.
            // Subscriber endpoints treat a null/missing instance ID as "no active workflow"
            // and return 200 OK immediately, which is the correct behaviour in tests.
            services.RemoveAll<DaprClient>();
            var stubDaprClient = Substitute.For<DaprClient>();
            stubDaprClient
                .GetStateAsync<string?>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ConsistencyMode?>(),
                    Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>(null));
            services.AddSingleton(stubDaprClient);
        });
    }
}
