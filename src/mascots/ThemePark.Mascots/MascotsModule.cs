using Microsoft.Extensions.DependencyInjection;
using ThemePark.Mascots.Features.ClearMascot;
using ThemePark.Mascots.Features.GetMascots;
using ThemePark.Mascots.Features.SimulateIntrusion;

namespace ThemePark.Mascots;

public static class MascotsModule
{
    public static IServiceCollection AddMascotsModule(this IServiceCollection services)
    {
        services.AddScoped<GetMascotsHandler>();
        services.AddScoped<ClearMascotHandler>();
        services.AddScoped<SimulateIntrusionHandler>();
        return services;
    }
}
