using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;
using TechBazar.Models;
using TechBazar.Services;

namespace TechBazar.Controllers
{
    [Authorize(Policy = "ManageUsers")] // Only admins can manage permissions
    public class PermissionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionsController(
            ApplicationDbContext context,
            PermissionService permissionService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _permissionService = permissionService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Permissions
        public async Task<IActionResult> Index()
        {
            var permissions = await _context.Permissions.ToListAsync();
            return View(permissions);
        }

        // GET: Permissions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Permissions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Permission permission)
        {
            if (ModelState.IsValid)
            {
                // Check if permission already exists
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permission.Name);

                if (existingPermission != null)
                {
                    ModelState.AddModelError("Name", "A permission with this name already exists.");
                    return View(permission);
                }

                permission.CreatedDate = DateTime.Now;
                _context.Add(permission);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Permission '{permission.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(permission);
        }

        // GET: Permissions/ManageRoles
        public async Task<IActionResult> ManageRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var permissions = await _context.Permissions.ToListAsync();

            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .ToListAsync();

            ViewBag.Roles = roles;
            ViewBag.Permissions = permissions;
            ViewBag.RolePermissions = rolePermissions;

            // Set specific permission IDs for the Assign Product Permissions button
            var createPermission = permissions.FirstOrDefault(p => p.Name == "Products.Create");
            var updatePermission = permissions.FirstOrDefault(p => p.Name == "Products.Update");
            var deletePermission = permissions.FirstOrDefault(p => p.Name == "Products.Delete");

            // Debug logging
            Console.WriteLine($"Create Permission: {createPermission?.Name} - ID: {createPermission?.Id}");
            Console.WriteLine($"Update Permission: {updatePermission?.Name} - ID: {updatePermission?.Id}");
            Console.WriteLine($"Delete Permission: {deletePermission?.Name} - ID: {deletePermission?.Id}");

            ViewData["CreatePermissionId"] = createPermission?.Id;
            ViewData["UpdatePermissionId"] = updatePermission?.Id;
            ViewData["DeletePermissionId"] = deletePermission?.Id;

            return View();
        }

        // POST: Permissions/AssignPermission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPermission(string roleId, int permissionId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                var permission = await _context.Permissions.FindAsync(permissionId);

                if (role == null || permission == null)
                {
                    TempData["ErrorMessage"] = "Role or permission not found.";
                    return RedirectToAction(nameof(ManageRoles));
                }

                // Check if already assigned
                var existing = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (existing != null)
                {
                    TempData["ErrorMessage"] = $"Permission '{permission.Name}' is already assigned to role '{role.Name}'.";
                    return RedirectToAction(nameof(ManageRoles));
                }

                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    Role = role,
                    Permission = permission,
                    AssignedDate = DateTime.Now
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Permission '{permission.Name}' assigned to role '{role.Name}' successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning permission: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // POST: Permissions/RemovePermission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePermission(string roleId, int permissionId)
        {
            try
            {
                var rolePermission = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (rolePermission == null)
                {
                    TempData["ErrorMessage"] = "Role permission assignment not found.";
                    return RedirectToAction(nameof(ManageRoles));
                }

                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Permission removed from role successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error removing permission: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // POST: Permissions/UpdateRolePermissions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRolePermissions(string selectedRoleId)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedRoleId))
                {
                    TempData["ErrorMessage"] = "No role selected.";
                    return RedirectToAction(nameof(ManageRoles));
                }

                var role = await _roleManager.FindByIdAsync(selectedRoleId);
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Selected role not found.";
                    return RedirectToAction(nameof(ManageRoles));
                }

                var allPermissions = await _context.Permissions.ToListAsync();

                // Get the form data to check which permissions are selected
                var form = HttpContext.Request.Form;
                var selectedPermissionIds = new HashSet<int>();

                // Parse selected permissions from form data
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("permissions[") && key.EndsWith("]"))
                    {
                        var permissionIdStr = key.Substring(12, key.Length - 13); // Extract ID from "permissions[ID]"
                        if (int.TryParse(permissionIdStr, out var permissionId))
                        {
                            selectedPermissionIds.Add(permissionId);
                        }
                    }
                }

                // Update permissions for the role
                foreach (var permission in allPermissions)
                {
                    var existingRolePermission = await _context.RolePermissions
                        .FirstOrDefaultAsync(rp => rp.RoleId == selectedRoleId && rp.PermissionId == permission.Id);

                    var shouldHavePermission = selectedPermissionIds.Contains(permission.Id);

                    if (shouldHavePermission)
                    {
                        // Assign permission if not already assigned
                        if (existingRolePermission == null)
                        {
                            var rolePermission = new RolePermission
                            {
                                RoleId = selectedRoleId,
                                PermissionId = permission.Id,
                                Role = role,
                                Permission = permission,
                                AssignedDate = DateTime.Now
                            };
                            _context.RolePermissions.Add(rolePermission);
                        }
                    }
                    else
                    {
                        // Remove permission if assigned
                        if (existingRolePermission != null)
                        {
                            _context.RolePermissions.Remove(existingRolePermission);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Permissions for role '{role.Name}' updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating permissions: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // GET: Permissions/UserPermissions
        public async Task<IActionResult> UserPermissions()
        {
            var users = await _userManager.Users.ToListAsync();
            var userPermissions = new Dictionary<string, List<string>>();

            foreach (var user in users)
            {
                var permissions = await _permissionService.GetUserPermissionsAsync(user);
                userPermissions[user.Id] = permissions;
            }

            ViewBag.Users = users;
            ViewBag.UserPermissions = userPermissions;

            return View();
        }
    }
}
