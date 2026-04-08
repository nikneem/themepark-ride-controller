using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ThemePark.ControlCenter.Tests;

/// <summary>
/// WebApplicationFactory for integration tests against the Control Center API.
/// Removes Dapr workflow hosted services that require a live Dapr sidecar during startup.
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
        });
    }
}
