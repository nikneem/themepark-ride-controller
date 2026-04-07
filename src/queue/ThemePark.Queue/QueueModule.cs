using Microsoft.Extensions.DependencyInjection;
using ThemePark.Queue.Features.GetQueue;
using ThemePark.Queue.Features.LoadPassengers;
using ThemePark.Queue.Features.SimulateQueue;

namespace ThemePark.Queue;

public static class QueueModule
{
    public static IServiceCollection AddQueueModule(this IServiceCollection services)
    {
        services.AddScoped<GetQueueHandler>();
        services.AddScoped<LoadPassengersHandler>();
        services.AddScoped<SimulateQueueHandler>();
        return services;
    }
}
