using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IRepository<AppUser> UserRepository { get; }
        IRepository<UserRole> RoleRepository { get; }
        IRepository<UserRoleMapping> UserRoleMappingRepository { get; }
        //IRepository<Privilege> PrivilegeRepository { get; }
        //IRepository<RolePrivilege> RolePrivilegeRepository { get; }
        IRepository<Permission> PermissionRepository { get; }
        IRepository<RolePermission> RolePermissionRepository { get; }

        // Transaction Management
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> SaveChangesAsync(); // Optional (for non-transactional saves)
    }
}
