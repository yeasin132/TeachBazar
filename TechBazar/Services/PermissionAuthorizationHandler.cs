using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TechBazar.Models;

namespace TechBazar.Services
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var permissionService = scope.ServiceProvider.GetRequiredService<PermissionService>();

                var user = await userManager.GetUserAsync(context.User);
                if (user == null)
                {
                    return;
                }

                if (await permissionService.HasPermissionAsync(user, requirement.Permission))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
