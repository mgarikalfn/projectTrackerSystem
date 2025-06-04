//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using FluentResults;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using projectTracker.Application.Interfaces;
//using projectTracker.Domain.Entities;
//using ProjectTracker.Infrastructure.Data;

//namespace projectTracker.Infrastructure.Services
//{
//    public class RolePrivilegeRepository : Repository<RolePrivileges>, IRolePrivilegeRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly ILogger<RolePrivilegeRepository> _logger;

//        public RolePrivilegeRepository(
//            AppDbContext context,
//            ILogger<RolePrivilegeRepository> logger) : base(context)
//        {
//            _context = context ?? throw new ArgumentNullException(nameof(context));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public async Task<Result<int>> AssignPrivilegesToRoleAsync(
//    string roleId,
//    List<int> privilegeIds,
//    CancellationToken cancellationToken = default)
//        {
//            await using var transaction = await _context.Database.BeginTransactionAsync(
//                System.Data.IsolationLevel.Serializable,
//                cancellationToken);

//            try
//            {
//                // 1. Verify inputs
//                if (string.IsNullOrEmpty(roleId))
//                    return Result.Fail<int>("Role ID cannot be empty");

//                if (privilegeIds == null || !privilegeIds.Any())
//                    return Result.Fail<int>("No privileges provided");

//                // 2. Verify existence (using single query for efficiency)
//                var exists = await _context.Roles
//                    .Where(r => r.Id == roleId)
//                    .Select(r => new
//                    {
//                        RoleExists = true,
//                        PrivilegeCount = _context.Privileges
//                            .Count(p => privilegeIds.Contains(p.Id))
//                    })
//                    .FirstOrDefaultAsync(cancellationToken);

//                if (exists == null)
//                    return Result.Fail<int>($"Role {roleId} does not exist");

//                if (exists.PrivilegeCount != privilegeIds.Count)
//                    return Result.Fail<int>("Some privileges don't exist");

//                // 3. Get current mappings (simplified)
//                var currentPrivilegeIds = await _context.RolePrivileges
//                    .Where(rp => rp.RoleId == roleId)
//                    .Select(rp => rp.PrivilegeId)
//                    .ToListAsync(cancellationToken);

//                // 4. Calculate changes
//                var toRemoveIds = currentPrivilegeIds.Except(privilegeIds).ToList();
//                var toAddIds = privilegeIds.Except(currentPrivilegeIds).ToList();

//                // 5. Execute changes
//                if (toRemoveIds.Any())
//                {
//                    _context.RolePrivileges.RemoveRange(
//                        toRemoveIds.Select(id => new RolePrivileges
//                        {
//                            RoleId = roleId,
//                            PrivilegeId = id
//                        }));
//                }

//                if (toAddIds.Any())
//                {
//                    await _context.RolePrivileges.AddRangeAsync(
//                        toAddIds.Select(id => new RolePrivileges
//                        {
//                            RoleId = roleId,
//                            PrivilegeId = id
//                        }),
//                        cancellationToken);
//                }

//                // 6. Final validation
//                var changes = await _context.SaveChangesAsync(cancellationToken);
//                await transaction.CommitAsync(cancellationToken);

//                return Result.Ok(changes);
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync(cancellationToken);
//                _logger.LogError(ex, "Failed to assign privileges to role {RoleId}", roleId);
//                return Result.Fail<int>("Operation failed").WithError(ex.Message);
//            }
//        }
//    }
    
//}