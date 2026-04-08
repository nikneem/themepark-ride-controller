using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace ThemePark.IntegrationTests.Harness;

public sealed class AppHostFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("App not started.");

    public HttpClient CreateHttpClient(string resourceName) =>
        App.CreateHttpClient(resourceName);

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ThemePark_Aspire_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
