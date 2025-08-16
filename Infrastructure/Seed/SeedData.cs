using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Seed;

public class SeedData(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, DataContext context)
{
    public async Task<bool> SeedCenter()
    {
        if (await context.Centers.AnyAsync())
            return false;
        var defaultCenter = new Center
        {
            Name = "Main Office",
            Address = "Dushanbe, Main Street",
            Description = "The main center for our organization",
            StudentCapacity = 100,
            IsActive = true,
            ContactPhone = "+992 000 00 00",
            Email = "contact@example.com",
        };
        
        await context.Centers.AddAsync(defaultCenter);
        await context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> SeedUser()
    {
        var existing = await userManager.FindByNameAsync("superadmin");
        if (existing != null)
        {
            if (!await userManager.IsInRoleAsync(existing, Roles.SuperAdmin))
                await userManager.AddToRoleAsync(existing, Roles.SuperAdmin);
            return false;
        }

        var user = new User()
        {
            UserName = "superadmin",
            Email = "superadmin@example.com",
            Address = "Dushanbe",
            Age = 30,
            Gender = 0,
            ProfileImagePath = "null",
            FullName = "Super Admin",
            PhoneNumber = "987654321",
            CenterId = null
        };

        var result = await userManager.CreateAsync(user, "Qwerty123!");
        if (!result.Succeeded) return false;

        var addToRoleResult = await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        if (!addToRoleResult.Succeeded)
        {
            // 
        }
        return true;
    }
    
    public async Task<bool> SeedRole()
    {
        var newRoles = new List<IdentityRole<int>>()
        {
            new (Roles.Admin),
            new (Roles.Manager),
            new (Roles.SuperAdmin),
            new (Roles.Student),
            new (Roles.Teacher),
        };
        
        var roles = await roleManager.Roles.ToListAsync();
        
        foreach (var role in newRoles)
        {
            if (roles.Any(e => e.Name == role.Name))
                continue;

            await roleManager.CreateAsync(role);
        }

        return true;
    }
    
    public async Task<bool> SeedManager()
    {
        var existingManager = await userManager.FindByNameAsync("manager1");
        if (existingManager != null)
        {
            if (!await userManager.IsInRoleAsync(existingManager, Roles.Manager))
                await userManager.AddToRoleAsync(existingManager, Roles.Manager);
            return false;
        }

        var center = await context.Centers.FirstOrDefaultAsync();
        if (center == null)
            return false;

        var manager = new User
        {
            UserName = "manager1",
            Email = "manager1@example.com",
            Address = "Dushanbe",
            Age = 28,
            Gender = 0,
            ProfileImagePath = null,
            FullName = "Manager One",
            PhoneNumber = "900000001",
            CenterId = center.Id,
            ActiveStatus = Domain.Enums.ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(manager, "Manager123!");
        if (!result.Succeeded) return false;

        var addToRoleResult = await userManager.AddToRoleAsync(manager, Roles.Manager);
        if (!addToRoleResult.Succeeded) return false;
        center.ManagerId = manager.Id;
        context.Centers.Update(center);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task SeedAllData()
    {
        await SeedRole();
        await SeedCenter();
        await SeedUser();
        await SeedManager();
    }
}