using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;
using TechBazar.Models;
using TechBazar.Services;

namespace TechBazar.Controllers
{
    public class DebugController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PermissionService _permissionService;

        public DebugController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, PermissionService permissionService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> CheckData()
        {
            var categories = await _context.Categories.ToListAsync();
            var products = await _context.Products.Include(p => p.Category).ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Products = products;

            return View();
        }

        public async Task<IActionResult> CheckUsers()
        {
            var users = _userManager.Users.ToList();
            var roles = _roleManager.Roles.ToList();

            ViewBag.Users = users;
            ViewBag.Roles = roles;

            return View();
        }

        public async Task<IActionResult> CheckPermissions()
        {
            var users = await _userManager.Users.ToListAsync();
            var permissions = await _context.Permissions.ToListAsync();
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .ToListAsync();

            var userPermissions = new Dictionary<string, List<string>>();
            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                var perms = await _permissionService.GetUserPermissionsAsync(user);
                userPermissions[user.Email] = perms;

                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Email] = roles.ToList();
            }

            ViewBag.Users = users;
            ViewBag.Permissions = permissions;
            ViewBag.RolePermissions = rolePermissions;
            ViewBag.UserPermissions = userPermissions;
            ViewBag.UserRoles = userRoles;

            return View();
        }
    }
}
