using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using projectTracker.Application.Interfaces;

namespace projectTracker.Infrastructure.BackgroundTask
{
    public class JiraSyncService : BackgroundService
    {
        private readonly ILogger<JiraSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public JiraSyncService(
            IServiceProvider serviceProvider,
            ILogger<JiraSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Jira Sync Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var syncManager = scope.ServiceProvider.GetRequiredService<ISyncManager>();
                        await syncManager.SyncAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during sync");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}