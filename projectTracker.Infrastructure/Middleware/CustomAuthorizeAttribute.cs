using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using projectTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using ProjectTracker.Infrastructure.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace projectTracker.Infrastructure.Middleware
{
    public class CustomAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            // Skip if AllowAnonymous is present
            if (context.ActionDescriptor.EndpointMetadata.Any(em => em is IAllowAnonymous))
                return;

            try
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CustomAuthorizeAttribute>>();

                // 1. Get user principal from the validated token
                var principal = context.HttpContext.User;
                if (principal?.Identity?.IsAuthenticated != true)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // 2. Get user ID and roles
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                // 3. Determine required privilege from controller/action
                if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                var requiredPrivilege = $"{descriptor.ControllerName.Replace("Controller", "")}-{descriptor.ActionName}";

                // 4. Check database for role-privilege mapping with detailed logging
                logger.LogInformation($"Checking privilege for user {userId} with roles: {string.Join(", ", userRoles)}");
                logger.LogInformation($"Required privilege: {requiredPrivilege}");

                // Step 1: Get role IDs from names
                var roleIds = await dbContext.Roles
                    .Where(r => userRoles.Contains(r.Name))
                    .Select(r => r.Id)
                    .ToListAsync();

                // Step 2: Use those IDs to get privileges
                var userPrivileges = await dbContext.RolePrivileges
                    .Where(rp => roleIds.Contains(rp.RoleId))
                    .Include(rp => rp.Privilage)
                    .Select(rp => rp.Privilage.PrivilageName)
                    .ToListAsync();


                logger.LogInformation($"User has access to these privileges: {string.Join(", ", userPrivileges)}");

                // Now check for the specific required privilege
                var hasPrivilege = userPrivileges.Contains(requiredPrivilege);

                if (!hasPrivilege)
                {
                    // Additional debug: Check if the privilege exists at all in the system
                    var privilegeExists = await dbContext.Privileges
                        .AnyAsync(p => p.PrivilageName == requiredPrivilege);

                    logger.LogWarning($"Access denied - User {userId} lacks privilege {requiredPrivilege}");
                    logger.LogWarning($"Privilege exists in system: {privilegeExists}");

                    // Check role assignments for this privilege
                    if (privilegeExists)
                    {
                        var rolesWithPrivilege = await dbContext.RolePrivileges
                            .Where(rp => rp.Privilage.PrivilageName == requiredPrivilege)
                            .Select(rp => rp.RoleId)
                            .ToListAsync();

                        logger.LogWarning($"This privilege is assigned to roles: {string.Join(", ", rolesWithPrivilege)}");
                    }

                    context.Result = new ForbidResult();
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CustomAuthorizeAttribute>>();
                logger.LogError(ex, "Authorization failed");
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

