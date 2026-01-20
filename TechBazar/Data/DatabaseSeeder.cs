using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;

namespace TechBazar.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDatabaseAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                Console.WriteLine("=== STARTING DATABASE SETUP ===");

                try
                {
                    // Check if there are pending migrations
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine($"Applying {pendingMigrations.Count()} pending migrations...");
                        await context.Database.MigrateAsync();
                        Console.WriteLine("✅ Database migrations applied successfully");
                    }
                    else
                    {
                        Console.WriteLine("✅ No pending migrations");
                    }

                    // Ensure roles exist
                    await EnsureRolesAsync(roleManager);

                    // Ensure admin user exists
                    await EnsureAdminUserAsync(userManager);

                    // Ensure manager user exists
                    await EnsureManagerUserAsync(userManager);

                    // Seed data if needed
                    SeedData.Initialize(context);

                    Console.WriteLine("=== DATABASE SETUP COMPLETED ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERROR during database setup: {ex.Message}");
                    throw;
                }
            }
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Manager", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"✅ Created role: {roleName}");
                }
                else
                {
                    Console.WriteLine($"✅ Role already exists: {roleName}");
                }
            }
        }

        private static async Task EnsureAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            var adminEmail = "admin@techbazaar.com";
            var adminPassword = "Admin@123";

            Console.WriteLine($"Checking admin user: {adminEmail}");

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                Console.WriteLine("Admin user not found, creating...");
                var user = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine($"✅ Admin user created: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("Admin user found, checking roles...");
                var roles = await userManager.GetRolesAsync(adminUser);
                if (!roles.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"✅ Admin role added to existing user: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"✅ Admin user already has Admin role: {adminEmail}");
                }
            }
        }

        private static async Task EnsureManagerUserAsync(UserManager<IdentityUser> userManager)
        {
            var managerEmail = "manager@techbazaar.com";
            var managerPassword = "Manager@123";

            Console.WriteLine($"=== MANAGER USER SETUP ===");
            Console.WriteLine($"Checking manager user: {managerEmail}");

            var managerUser = await userManager.FindByEmailAsync(managerEmail);

            if (managerUser == null)
            {
                Console.WriteLine("❌ Manager user not found, creating...");
                var user = new IdentityUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, managerPassword);
                if (result.Succeeded)
                {
                    Console.WriteLine($"✅ Manager user account created");
                    await userManager.AddToRoleAsync(user, "Manager");
                    Console.WriteLine($"✅ Manager role assigned");

                    // Verify the user was created and role was assigned
                    var verifyUser = await userManager.FindByEmailAsync(managerEmail);
                    if (verifyUser != null)
                    {
                        var verifyRoles = await userManager.GetRolesAsync(verifyUser);
                        Console.WriteLine($"✅ Manager user verification - Roles: {string.Join(", ", verifyRoles)}");

                        // Test the password
                        var passwordCheck = await userManager.CheckPasswordAsync(verifyUser, managerPassword);
                        Console.WriteLine($"✅ Password verification: {passwordCheck}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ FAILED to create manager user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("✅ Manager user found, checking roles...");
                var roles = await userManager.GetRolesAsync(managerUser);
                Console.WriteLine($"Current roles: {string.Join(", ", roles)}");

                if (!roles.Contains("Manager"))
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                    Console.WriteLine($"✅ Manager role added to existing user: {managerEmail}");
                }
                else
                {
                    Console.WriteLine($"✅ Manager user already has Manager role: {managerEmail}");
                }

                // Test the password
                var passwordCheck = await userManager.CheckPasswordAsync(managerUser, managerPassword);
                Console.WriteLine($"✅ Password verification: {passwordCheck}");
            }
            Console.WriteLine($"=== MANAGER SETUP COMPLETED ===");
        }
    }
}