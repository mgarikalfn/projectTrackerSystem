using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private bool _disposed;

        // Repositories (lazy-loaded)
        private IRepository<AppUser>? _userRepository;
        private IRepository<UserRole>? _roleRepository;
        private IRepository<UserRoleMapping>? _userRoleMappingRepository;
        private IRepository<Permission>? _permissionRepository;
        private IRepository<RolePermission>? _rolePermissionRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // Repository Properties
        public IRepository<AppUser> UserRepository =>
            _userRepository ??= new Repository<AppUser>(_context) as IRepository<AppUser>;

        public IRepository<UserRole> RoleRepository =>
            _roleRepository ??= new Repository<UserRole>(_context) as IRepository<UserRole>;

        public IRepository<UserRoleMapping> UserRoleMappingRepository =>
            _userRoleMappingRepository ??= new Repository<UserRoleMapping>(_context) as IRepository<UserRoleMapping>;

        public IRepository<Permission> PermissionRepository =>
           _permissionRepository ??= new Repository<Permission>(_context) as IRepository<Permission>;

        public IRepository<RolePermission> RolePermissionRepository =>
           _rolePermissionRepository ??= new Repository<RolePermission>(_context) as IRepository<RolePermission>;

        // Transaction Methods
        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        // Save Changes (optional)
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Dispose
        public void Dispose()
        {
            if (!_disposed)
            {
                _context.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
