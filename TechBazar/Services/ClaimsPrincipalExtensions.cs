using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TechBazar.Data;
using TechBazar.Services;
using TechBazar.Models;

namespace TechBazar.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static async Task<bool> HasPermission(this ClaimsPrincipal user, string permissionName, PermissionService permissionService, UserManager<ApplicationUser> userManager)
        {
            if (!user.Identity.IsAuthenticated)
                return false;

            var applicationUser = await userManager.GetUserAsync(user);
            if (applicationUser == null)
                return false;

            return await permissionService.HasPermissionAsync(applicationUser, permissionName);
        }
    }
}
