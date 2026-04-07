using ThemePark.Mascots.Api.Models;

namespace ThemePark.Mascots.Api.SimulateIntrusion;

public static class SimulateIntrusionEndpoint
{
    public static IEndpointRouteBuilder MapSimulateIntrusion(this IEndpointRouteBuilder app, IConfiguration config)
    {
        if (!config.GetValue<bool>("Dapr:DemoMode"))
            return app;

        app.MapPost("/mascots/simulate-intrusion",
            (SimulateIntrusionRequest request, SimulateIntrusionHandler handler, CancellationToken ct) =>
                handler.HandleAsync(request, ct));

        return app;
    }
}
