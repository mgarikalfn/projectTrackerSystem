using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using projectTracker.Infrastructure.Sync;
using projectTracker.Infrastructure.Adapter;
using System.Threading;

namespace projectTracker.Infrastructure.Sync
{
    public class SyncManager : ISyncManager
    {
        private readonly AppDbContext _dbContext;
        private readonly IProjectManegementAdapter _adapter;
        private readonly ILogger<SyncManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRiskCalculatorService _riskCalculator;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<UserRole> _roleManager;

        private readonly SyncUsers _syncUsers;
        private readonly SyncProjects _syncProjects;
        private readonly SyncBoardsAndSprints _syncBoardsAndSprints;
        private readonly SyncTasks _syncTasks;

        public SyncManager(
            AppDbContext dbContext,
            IProjectManegementAdapter adapter,
            ILogger<SyncManager> logger,
            IRiskCalculatorService riskCalculator,
            UserManager<AppUser> userManager,
            RoleManager<UserRole> roleManager,
            ILoggerFactory loggerFactory)
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _riskCalculator = riskCalculator;
            _userManager = userManager;
            _roleManager = roleManager;

            // Ensure the adapter is JiraAdapter  
            if (_adapter is not JiraAdapter jiraAdapter)
                throw new InvalidOperationException("Adapter must be of type JiraAdapter.");

          
            _syncUsers = new SyncUsers(_dbContext, jiraAdapter, _userManager, _roleManager, loggerFactory.CreateLogger<SyncUsers>());

            _syncProjects = new SyncProjects(_dbContext, jiraAdapter, _riskCalculator, loggerFactory.CreateLogger<SyncProjects>());

          
            _syncBoardsAndSprints = new SyncBoardsAndSprints(_dbContext, jiraAdapter, loggerFactory.CreateLogger<SyncBoardsAndSprints>());

            _syncTasks = new SyncTasks(loggerFactory.CreateLogger<SyncTasks>(), _dbContext, jiraAdapter);
        }

        public async Task SyncAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting full sync...");

            var syncStartTime = DateTime.UtcNow;
            var syncHistory = SyncHistory.Start(
                type: SyncType.Full,
                projectId: null,
                trigger: "Manual/Scheduled"
            );
            _dbContext.Set<SyncHistory>().Add(syncHistory);
            await _dbContext.SaveChangesAsync(ct);

            try
            {
              //  _logger.LogInformation("Executing User Synchronization...");
               // await SyncUsersAsync(ct);

               // _logger.LogInformation("Executing Project Synchronization...");
               // await SyncProjectsAsync(ct);

               // _logger.LogInformation("Executing Board and Sprint Synchronization...");
               // await SyncBoardsAndSprintsAsync(ct);

                _logger.LogInformation("Executing Task Synchronization...");
                var (created, updated) = await _syncTasks.ExecuteAsync(ct);

                //syncHistory.Complete(created, updated);
                //await _dbContext.SaveChangesAsync(ct);

               // _logger.LogInformation("Full sync completed successfully. Created: {Created}, Updated: {Updated}", created, updated);
            }
            catch (Exception ex)
            {
                syncHistory.Fail(ex.Message);
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogError(ex, "Failed to complete full sync");
                throw;
            }
        }

        private async Task SyncUsersAsync(CancellationToken ct)
        {
            await _syncUsers.ExecuteAsync(ct);
        }

        private async Task SyncProjectsAsync(CancellationToken ct)
        {
            await _syncProjects.ExecuteAsync(ct);
        }

        private async Task SyncBoardsAndSprintsAsync(CancellationToken ct)
        {
            await _syncBoardsAndSprints.ExecuteAsync(ct);
        }
    }
}
