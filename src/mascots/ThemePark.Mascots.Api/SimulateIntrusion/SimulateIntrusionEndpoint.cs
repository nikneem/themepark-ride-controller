using ThemePark.Mascots.Abstractions.DataTransferObjects;
using ThemePark.Mascots.Features.SimulateIntrusion;
using ThemePark.Shared;

namespace ThemePark.Mascots.Api.SimulateIntrusion;

public static class SimulateIntrusionEndpoint
{
    public static IEndpointRouteBuilder MapSimulateIntrusion(this IEndpointRouteBuilder app, IConfiguration config)
    {
        if (!config.GetValue<bool>("Dapr:DemoMode"))
            return app;

        app.MapPost("/mascots/simulate-intrusion",
            async (SimulateIntrusionRequest request, SimulateIntrusionHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Accepted()
                    : Results.BadRequest(new { error = result.Error });
            });

        return app;
    }
}

