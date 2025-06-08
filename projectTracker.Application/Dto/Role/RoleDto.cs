using System;
using System.Collections.Generic;

namespace projectTracker.Application.Dto.Role
{
    public record RoleDto(
        DateTime CreatedAt,
        string Name,
        string Description,
        List<string> Permissions,
        string RoleId
    );

    public record RolePermissionDto(
        string PermissionId,
        string PermissionName
        
    );

    //public class UpdateRoleDto
    //{
    //    public string? RoleName { get; set; }
    //    public string? Description { get; set; }
    //    public List<string> PermissionIdsToAdd = new List<string>();
    //}
}