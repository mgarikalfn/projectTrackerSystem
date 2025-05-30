using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Seeder
{
    public static class DbSeeder
    {
        public static async Task Seed(AppDbContext context)
        {
            if (!context.Users.Any())
            {
                var user = new AppUser { Id = "user-1", UserName = "Girum", Email = "Girum@gmail.com" };
                var role = new UserRole { Id = "role-1", Name = "Manager" };
                var privilege = new Privilege {  Action = "Project-GetProjectById" };

                context.Users.Add(user);
                context.UserRoles.Add(role);
                context.Privileges.Add(privilege);

                context.UserRoleMappings.Add(new UserRoleMapping { UserId = user.Id, RoleId = role.Id });
                context.RolePrivileges.Add(new RolePrivilege { RoleId = role.Id, PrivilageId = privilege.Id });

                await context.SaveChangesAsync();
            }
        }
    }

}
