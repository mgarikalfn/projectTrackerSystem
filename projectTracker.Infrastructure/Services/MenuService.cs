using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Dto;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Services
{
    // Services/MenuService.cs
    public class MenuService
    {
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MenuService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<MenuItemDto>> GetFilteredMenuAsync()
        {
            // 1. Get current user's roles from JWT
            var userRoles = _httpContextAccessor.HttpContext?.User
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (userRoles == null || !userRoles.Any())
                return new List<MenuItemDto>();

            // 2. Get all active menu items (optimized query)
            var allMenuItems = await _dbContext.MenuItems
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .ToListAsync();

            // 3. Filter by user privileges
            var rootItems = allMenuItems.Where(m => m.ParentId == null).ToList();
            var filteredMenu = new List<MenuItemDto>();

            foreach (var item in rootItems)
            {
                var filteredItem = await FilterMenuItemAsync(item, allMenuItems, userRoles);
                if (filteredItem != null)
                    filteredMenu.Add(filteredItem);
            }

            return filteredMenu;
        }

        private async Task<MenuItemDto?> FilterMenuItemAsync(
            MenuItem item,
            List<MenuItem> allItems,
            List<string> userRoles)
        {
            // 1. Check if user has the required privilege
            if (!string.IsNullOrEmpty(item.RequiredPrivilege))
            {
                var hasAccess = await CheckPermissionAsync(item.RequiredPrivilege, userRoles);
                if (!hasAccess)
                    return null;
            }

            // 2. Build DTO
            var dto = new MenuItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Url = item.Url,
                Icon = item.Icon,
                Children = new List<MenuItemDto>()
            };

            // 3. Recursively filter children
            var children = allItems.Where(m => m.ParentId == item.Id).ToList();
            foreach (var child in children)
            {
                var filteredChild = await FilterMenuItemAsync(child, allItems, userRoles);
                if (filteredChild != null)
                    dto.Children.Add(filteredChild);
            }

            // 4. Exclude if no children and no URL
            if (!dto.Children.Any() && string.IsNullOrEmpty(dto.Url))
                return null;

            return dto;
        }

        private async Task<bool> CheckPermissionAsync(string permission, List<string> userRoles)
        {
            // Reuse your existing permission check logic
            var roleIds = await _dbContext.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            return await _dbContext.RolePermissions
    .Where(rp => roleIds.Contains(rp.RoleId))
    .Join(_dbContext.Permissions,
          rp => rp.PermissionId,
          p => p.Id,
          (rp, p) => p)
    .AnyAsync(p => p.Action == permission);

        }
    }
}
