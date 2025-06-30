using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Sync
{
    public class SyncUsers
    {
        private readonly IProjectManegementAdapter _adapter;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<UserRole> _roleManager;
        private readonly ILogger<SyncUsers> _logger;

        public SyncUsers(
            AppDbContext dbContext,
            IProjectManegementAdapter adapter,
            UserManager<AppUser> userManager,
            RoleManager<UserRole> roleManager,
            ILogger<SyncUsers> logger)
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting user sync...");
            try
            {
                var jiraUsers = await _adapter.GetAppUsersAsync(ct);
                _logger.LogDebug("Retrieved {UserCount} users from Jira", jiraUsers.Count);

                // Fetch existing users with tracking (removed AsNoTracking)
                var existingUsers = await _dbContext.Users.ToListAsync(ct);

                // Create lookup dictionaries
                var existingUserMapByEmail = existingUsers
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .ToDictionary(u => u.Email!, StringComparer.OrdinalIgnoreCase);

                var existingUserMapByAccountId = existingUsers
                    .Where(u => !string.IsNullOrEmpty(u.AccountId))
                    .ToDictionary(u => u.AccountId!, StringComparer.OrdinalIgnoreCase);

                foreach (var jiraUser in jiraUsers)
                {
                    try
                    {
                        AppUser? user = null;
                        bool isNewUser = false;

                        // Try to find by AccountId first
                        if (!string.IsNullOrEmpty(jiraUser.AccountId) &&
                            existingUserMapByAccountId.TryGetValue(jiraUser.AccountId, out var foundByAccountId))
                        {
                            user = foundByAccountId;
                            _logger.LogDebug("Found existing user by AccountId: {AccountId}", jiraUser.AccountId);
                        }
                        // Then try by email
                        else if (!string.IsNullOrWhiteSpace(jiraUser.Email) &&
                                existingUserMapByEmail.TryGetValue(jiraUser.Email, out var foundByEmail))
                        {
                            user = foundByEmail;
                            _logger.LogDebug("Found existing user by email: {Email}", jiraUser.Email);
                        }

                        if (user == null)
                        {
                            // Create new user
                            string userEmailToSet = string.IsNullOrWhiteSpace(jiraUser.Email)
                                ? $"{jiraUser.AccountId ?? Guid.NewGuid().ToString()[..8]}@jira-unassigned.invalid"
                                : jiraUser.Email;

                            user = new AppUser
                            {
                                UserName = jiraUser.AccountId,
                                Email = userEmailToSet,
                                EmailConfirmed = !string.IsNullOrWhiteSpace(jiraUser.Email),
                                AccountId = jiraUser.AccountId,
                                DisplayName = jiraUser.DisplayName,
                                AvatarUrl = jiraUser.AvatarUrl,
                                IsActive = jiraUser.Active,
                                Source = UserSource.Jira,
                                FirstName = string.Empty,
                                LastName = string.Empty
                            };

                            var createResult = await _userManager.CreateAsync(user, "TempPass!123");
                            if (!createResult.Succeeded)
                            {
                                _logger.LogError("Failed to create new Jira user {AccountId} ({Email}): {Errors}",
                                    jiraUser.AccountId, jiraUser.Email,
                                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                                continue;
                            }
                            isNewUser = true;
                            _logger.LogDebug("Added new Jira user: {AccountId} ({Email})",
                                jiraUser.AccountId, jiraUser.Email);
                        }
                        else
                        {
                            // Update existing user
                            if (user.Source == UserSource.Local ||
                                (user.AccountId == null && !string.IsNullOrEmpty(jiraUser.AccountId)))
                            {
                                user.Source = UserSource.Jira;
                                user.AccountId = jiraUser.AccountId;
                            }

                            // Update properties
                            user.DisplayName = jiraUser.DisplayName;
                            user.AvatarUrl = jiraUser.AvatarUrl;
                            user.IsActive = jiraUser.Active;

                            string newEmailToSet = string.IsNullOrWhiteSpace(jiraUser.Email)
                                ? $"{jiraUser.AccountId ?? user.AccountId ?? Guid.NewGuid().ToString()[..8]}@jira-unassigned.invalid"
                                : jiraUser.Email;

                            if (user.Email != newEmailToSet)
                            {
                                user.Email = newEmailToSet;
                                user.EmailConfirmed = !string.IsNullOrWhiteSpace(jiraUser.Email);
                            }

                            if (user.UserName != jiraUser.AccountId)
                            {
                                user.UserName = jiraUser.AccountId;
                            }

                            var updateResult = await _userManager.UpdateAsync(user);
                            if (!updateResult.Succeeded)
                            {
                                _logger.LogError("Failed to update user {AccountId} ({Email}): {Errors}",
                                    jiraUser.AccountId, jiraUser.Email,
                                    string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                                continue;
                            }
                            _logger.LogDebug("Updated existing user: {AccountId} ({Email})",
                                jiraUser.AccountId, jiraUser.Email);
                        }

                        // Assign default role to new users
                        if (isNewUser)
                        {
                            const string defaultRole = "Team Member";
                            if (await _roleManager.RoleExistsAsync(defaultRole))
                            {
                                if (!await _userManager.IsInRoleAsync(user, defaultRole))
                                {
                                    var addToRoleResult = await _userManager.AddToRoleAsync(user, defaultRole);
                                    if (!addToRoleResult.Succeeded)
                                    {
                                        _logger.LogError("Failed to assign role to {AccountId}: {Errors}",
                                            jiraUser.AccountId,
                                            string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Default role '{DefaultRole}' doesn't exist", defaultRole);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Jira user {AccountId} during sync",
                            jiraUser.AccountId);
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("User sync completed successfully. Processed {UserCount} users",
                    jiraUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete user sync");
                throw;
            }
        }
    }
}