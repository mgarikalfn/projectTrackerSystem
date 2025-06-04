using Microsoft.Extensions.DependencyInjection;
using projectTracker.Application.Interfaces;
using projectTracker.Infrastructure.Adapter;
using projectTracker.Infrastructure.Risk;
using projectTracker.Infrastructure.Services;
using projectTracker.Infrastructure.Sync;

namespace projectTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISyncManager, SyncManager>(); // Ensure SyncManager is a class, not a namespace
        services.AddScoped<IProjectManegementAdapter, JiraAdapter>();
        services.AddScoped<IRiskCalculatorService, RiskCalculationService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<MenuService>();

        return services;
    }
}
