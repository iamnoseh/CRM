using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Seed;

public class SeedData(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, DataContext context)
{
    public async Task<bool> SeedCenter()
    {
        // Check if any centers already exist
        if (await context.Centers.AnyAsync())
            return false;
            
        // Create a default center
        var defaultCenter = new Center
        {
            Name = "Main Office",
            Address = "Dushanbe, Main Street",
            Description = "The main center for our organization",
            StudentCapacity = 100,
            IsActive = true,
            ContactPhone = "+992 000 00 00",
            Email = "contact@example.com",
            ManagerName = "Admin"
        };
        
        await context.Centers.AddAsync(defaultCenter);
        await context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> SeedUser()
    {
        var existing = await userManager.FindByNameAsync("admin1234");
        if (existing != null) return false;
        
        // Find the first center or set CenterId to null
        var centerId = await context.Centers.Select(c => c.Id).FirstOrDefaultAsync();
        
        var user = new User()
        {
            UserName = "admin1234",
            Email = "nosehtagaymurodzoda@gmail.com",
            Address = "Dushanbe",
            Age = 24,
            Gender = 0,
            FullName = "Admin",
            PhoneNumber = "987654321",
            CenterId = centerId > 0 ? centerId : null
        };

        var result = await userManager.CreateAsync(user, "Qwerty123!");
        if (!result.Succeeded) return false;
        
        await userManager.AddToRoleAsync(user, Roles.Admin);
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
    
    public async Task SeedAllData()
    {
        // Order is important: first seed Center, then User, then Roles
        await SeedCenter();
        await SeedUser();
        await SeedRole();
    }
}