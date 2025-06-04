using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RolePermissionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RoleManager<UserRole> _roleManager;

    public RolePermissionsController(
        AppDbContext context,
        RoleManager<UserRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    // POST: api/rolepermissions
    [HttpPost]
    public async Task<ActionResult> AssignPermissionToRole(AssignPermissionDto dto)
    {
        // Verify role exists
        var role = await _roleManager.FindByIdAsync(dto.RoleId);
        if (role == null)
        {
            return BadRequest("Role does not exist");
        }

        // Verify permission exists
        var permission = await _context.Permissions.FindAsync(dto.PermissionId);
        if (permission == null)
        {
            return BadRequest("Permission does not exist");
        }

        // Check if assignment already exists
        var exists = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

        if (exists)
        {
            return Conflict("This permission is already assigned to the role");
        }

        var rolePermission = new RolePermission
        {
            RoleId = dto.RoleId,
            PermissionId = dto.PermissionId
        };

        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPost("assign-role-to-privileges")]
    public async Task<IActionResult> AssignRoletoPrivilages(RolePermissionDto rolePermissionDto)
    {

        var userrole = await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(a => a.Id == rolePermissionDto.RoleId);
        if (userrole == null)
        {

            return BadRequest(new JsonResult("Role Does not exist !"));
        }
        else
        {

            var existingPermissions = userrole.RolePermissions.Select(a => a.PermissionId).ToList();
            rolePermissionDto.PermissionIds = rolePermissionDto.PermissionIds.Where(s => !existingPermissions.Contains(s)).ToList();
            var newRolePermission = rolePermissionDto.PermissionIds.Select(permissionId => new RolePermission
            {
                PermissionId = permissionId,
                RoleId = userrole.Id
            }).ToList();
            if (newRolePermission.Count > 0)
                await _context.RolePermissions.AddRangeAsync(newRolePermission);
            if (await _context.SaveChangesAsync() > 0) return Ok(new JsonResult("Privilege is Assigned to Role"));
            else return BadRequest("User role is alredy assigned");
        }

    }




}

public class AssignPermissionDto
{
    [Required]
    public string RoleId { get; set; }

    [Required]
    public string PermissionId { get; set; }
}

public class RolePermissionDto
{

    public string RoleId { get; set; } = string.Empty;

    public List<string> PermissionIds { get; set; } = [];
}