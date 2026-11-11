using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Seeders;

public static class RoleSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        db.Roles.AddRange(
            new Role { Name = "Administrator" },
            new Role { Name = "Employee" },
            new Role { Name = "DepartmentManager" },
            new Role { Name = "FinanceOfficer" });
        await db.SaveChangesAsync();
    }
}