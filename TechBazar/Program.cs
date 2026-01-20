using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechBazar.Data;
using TechBazar.Models;
using TechBazar.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity Services with roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configure cookie settings for authorization
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<IMultiLanguageService, MultiLanguageService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Products.Manage", policy =>
        policy.Requirements.Add(new PermissionRequirement("Products.Manage")));
    options.AddPolicy("Products.Create", policy =>
        policy.Requirements.Add(new PermissionRequirement("Products.Create")));
    options.AddPolicy("Products.Update", policy =>
        policy.Requirements.Add(new PermissionRequirement("Products.Update")));
    options.AddPolicy("Products.Delete", policy =>
        policy.Requirements.Add(new PermissionRequirement("Products.Delete")));
    options.AddPolicy("ManageUsers", policy =>
        policy.Requirements.Add(new PermissionRequirement("ManageUsers")));
});

// Register the authorization handler (keeping for future use if needed)
builder.Services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Apply migrations and seed data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        Console.WriteLine("=== STARTING DATABASE SETUP ===");

        // Apply migrations
        try
        {
            await context.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed (likely due to existing tables): {ex.Message}");
            Console.WriteLine("Continuing with existing database schema...");
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
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating or seeding the database.");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Local functions for database seeding
async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
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

async Task EnsureAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "admin@techbazaar.com";
    var adminPassword = "Admin@123";

    Console.WriteLine($"Checking admin user: {adminEmail}");

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        Console.WriteLine("Admin user not found, creating...");
        var user = new ApplicationUser
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

async Task EnsureManagerUserAsync(UserManager<ApplicationUser> userManager)
{
    var managerEmail = "manager@techbazaar.com";
    var managerPassword = "Manager@123";

    Console.WriteLine($"=== MANAGER USER SETUP ===");
    Console.WriteLine($"Checking manager user: {managerEmail}");

    var managerUser = await userManager.FindByEmailAsync(managerEmail);

    if (managerUser == null)
    {
        Console.WriteLine("❌ Manager user not found, creating...");
        var user = new ApplicationUser
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
