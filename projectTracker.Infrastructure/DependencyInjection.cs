using Microsoft.Extensions.DependencyInjection;
using projectTracker.Application.Interfaces;
using projectTracker.Infrastructure.Adapter;
using projectTracker.Infrastructure.Risk;
using projectTracker.Infrastructure.Services;
using projectTracker.Infrastructure.SyncManager;

namespace YourProject.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISyncManager, SyncManager>();
        services.AddScoped<IProjectManegementAdapter, JiraAdapter>();
        services.AddScoped<IRiskCalculatorService, RiskCalculationService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
