using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;
using ProjectTracker.Infrastructure.Services;

namespace projectTracker.Infrastructure.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private bool _disposed;

        // Repositories (lazy-loaded)
        private IRepository<Privilege>? _userRepository;
        private IRepository<UserRole>? _roleRepository;
        private IRepository<UserRoleMapping>? _userRoleMappingRepository;
        private IRepository<Privilege>? _privilegeRepository;
        private IRepository<RolePrivilege>? _rolePrivilegeRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // Repository Properties
        public IRepository<Privilege> UserRepository =>
            _userRepository ??= new Repository<Privilege>(_context) as IRepository<Privilege>;

        public IRepository<UserRole> RoleRepository =>
            _roleRepository ??= new Repository<UserRole>(_context) as IRepository<UserRole>;

        public IRepository<UserRoleMapping> UserRoleMappingRepository =>
            _userRoleMappingRepository ??= new Repository<UserRoleMapping>(_context) as IRepository<UserRoleMapping>;

        public IRepository<Privilege> PrivilegeRepository =>
           _privilegeRepository ??= new Repository<Privilege>(_context) as IRepository<Privilege>;

        public IRepository<RolePrivilege> RolePrivilegeRepository =>
           _rolePrivilegeRepository ??= new Repository<RolePrivilege>(_context) as IRepository<RolePrivilege>;

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
