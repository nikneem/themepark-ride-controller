using Microsoft.Extensions.DependencyInjection;
using ThemePark.Refunds.Features.GetRefundHistory;
using ThemePark.Refunds.Features.IssueRefund;

namespace ThemePark.Refunds;

public static class RefundsModule
{
    public static IServiceCollection AddRefundsModule(this IServiceCollection services)
    {
        services.AddScoped<IssueRefundHandler>();
        services.AddScoped<GetRefundHistoryHandler>();
        return services;
    }
}
