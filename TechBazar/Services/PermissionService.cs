using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;
using TechBazar.Models;

namespace TechBazar.Services
{
    public class PermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> HasPermissionAsync(ApplicationUser user, string permissionName)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles == null || !userRoles.Any()) return false;

            var roleIds = await _context.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.Permission.Name == permissionName);

            return hasPermission;
        }

        public async Task<List<string>> GetUserPermissionsAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles == null || !userRoles.Any()) return new List<string>();

            var roleIds = await _context.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var permissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        public async Task AssignPermissionToRoleAsync(string roleName, string permissionName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) throw new ArgumentException($"Role '{roleName}' not found");

            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);
            if (permission == null) throw new ArgumentException($"Permission '{permissionName}' not found");

            var existing = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

            if (existing == null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    Role = role,
                    Permission = permission
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemovePermissionFromRoleAsync(string roleName, string permissionName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return;

            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);
            if (permission == null) return;

            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

            if (rolePermission != null)
            {
                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();
            }
        }
    }
}
