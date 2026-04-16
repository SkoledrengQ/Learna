using Learna.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Data;

public class DbSeeder
{
    private readonly ApplicationDbContext _context;

    public DbSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Ensure database is created
        await _context.Database.MigrateAsync();

        // Seed admin user if not exists
        await SeedAdminUserAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = "admin@learna.com";

        // Check if admin already exists
        var adminExists = await _context.Users.AnyAsync(u => u.Email == adminEmail);
        if (adminExists)
        {
            return; // Admin already exists
        }

        // Get Admin role
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            throw new InvalidOperationException("Admin role not found in database. Ensure migrations have been run.");
        }

        // Create admin user
        var adminUser = new User
        {
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        // Assign Admin role
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        Console.WriteLine($"Admin user created: {adminEmail} / Admin123!");
    }
}
