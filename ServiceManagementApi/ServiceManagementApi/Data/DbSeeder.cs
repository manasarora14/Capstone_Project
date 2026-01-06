using Microsoft.AspNetCore.Identity;
using ServiceManagementApi.Models;

namespace ServiceManagementApi.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        // CHANGE: Use ApplicationUser here
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Seed Roles
        string[] roles = { "Admin", "Manager", "Technician", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Seed Users with FullName
        var defaultPassword = "Test@1234";
        var usersToSeed = new List<(string Email, string Name, string Role)>
    {
        ("admin@gmail.com", "System Administrator", "Admin"),
        ("manager@gmail.com", "Service Manager", "Manager"),
        ("technician@gmail.com", "Technician", "Technician"),
        ("user@gmail.com", "User", "Customer")
    };

        foreach (var item in usersToSeed)
        {
            var user = await userManager.FindByEmailAsync(item.Email);
            if (user == null)
            {
                // CHANGE: Create ApplicationUser and assign FullName
                var newUser = new ApplicationUser
                {
                    UserName = item.Email,
                    Email = item.Email,
                    FullName = item.Name, // This ensures names show up immediately
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newUser, defaultPassword);
                if (result.Succeeded) await userManager.AddToRoleAsync(newUser, item.Role);
            }
        }

        // 3. Seed Categories (Remains the same)
        if (!context.ServiceCategories.Any())
        {
            context.ServiceCategories.AddRange(new List<ServiceCategory>
        {
            new ServiceCategory { Name = "Installation", Description = "Setup", BaseCharge = 500, SlaHours = 36 },
            new ServiceCategory { Name = "Maintenance", Description = "Checkups", BaseCharge = 200, SlaHours = 12 },
            new ServiceCategory { Name = "Repair", Description = "Fixing", BaseCharge = 350, SlaHours = 18 }
        });
            await context.SaveChangesAsync();
        }
    }
}