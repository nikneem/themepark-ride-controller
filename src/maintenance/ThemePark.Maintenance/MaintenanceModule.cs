using Microsoft.Extensions.DependencyInjection;
using ThemePark.Maintenance.Features.CompleteMaintenanceRequest;
using ThemePark.Maintenance.Features.CreateMaintenanceRequest;
using ThemePark.Maintenance.Features.GetMaintenanceHistory;

namespace ThemePark.Maintenance;

public static class MaintenanceModule
{
    public static IServiceCollection AddMaintenanceModule(this IServiceCollection services)
    {
        services.AddScoped<CreateMaintenanceRequestHandler>();
        services.AddScoped<CompleteMaintenanceRequestHandler>();
        services.AddScoped<GetMaintenanceHistoryHandler>();
        return services;
    }
}
