using Microsoft.Extensions.DependencyInjection;
using ThemePark.Rides.Features.GetRide;
using ThemePark.Rides.Features.PauseRide;
using ThemePark.Rides.Features.ResumeRide;
using ThemePark.Rides.Features.StartRide;
using ThemePark.Rides.Features.StopRide;

namespace ThemePark.Rides;

public static class RidesModule
{
    public static IServiceCollection AddRidesModule(this IServiceCollection services)
    {
        services.AddScoped<GetRideHandler>();
        services.AddScoped<StartRideHandler>();
        services.AddScoped<PauseRideHandler>();
        services.AddScoped<ResumeRideHandler>();
        services.AddScoped<StopRideHandler>();
        return services;
    }
}
