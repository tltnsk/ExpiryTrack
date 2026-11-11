using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Seeders;

public static class DepartmentSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var it = new Department { Name = "IT", isActive = true };
        db.Departments.Add(it);
        var marketing = new Department { Name = "Marketing", isActive = true };
        db.Departments.Add(marketing);
        var sales = new Department { Name = "Sales", isActive = true };
        db.Departments.Add(sales);
        var operations = new Department { Name = "Operations", isActive = true };
        db.Departments.Add(operations);
        var hr = new Department { Name = "HR", isActive = true };
        db.Departments.Add(hr);

        await db.SaveChangesAsync();
    }
}