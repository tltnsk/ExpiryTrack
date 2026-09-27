using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(AppDbContext db)
    {
        // if the roles are already seeded
        if (await db.Roles.AnyAsync())
            return;

        db.Roles.AddRange(
            new Role { Name = "Administator" },
            new Role { Name = "Employee" },
            new Role { Name = "DepartmentManager" },
            new Role { Name = "FinanceOfficer" });
        await db.SaveChangesAsync();
    }

    public static async Task SeedTestUsersAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        // convert the rows to dictionary and look up role ids by name
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);

        // create the first department since employee and manager roles must belong to a department
        var it = new Department { Name = "IT" };
        db.Departments.Add(it);
        await db.SaveChangesAsync();

        var admin = CreateUser("Test", "Admin", "admin@test.com", "Admin123!", roles["Administrator"], null);
        var employee = CreateUser("Test", "Employee", "employee@test.com", "Employee123!", roles["Employee"], it.Id);
        var manager = CreateUser("Test", "Manager", "manager@test.com", "Manager123!", roles["DepartmentManager"], it.Id);
        var finance = CreateUser("Test", "Finance", "finance@test.com", "Finance123!", roles["FinanceOfficer"], null);

        db.Users.AddRange(admin, employee, manager, finance);
        await db.SaveChangesAsync();

        // the manager has an id only after being saved
        it.ManagerId = manager.Id;
        await db.SaveChangesAsync();
    }

    // helper method for creating user objects 
    private static User CreateUser(string firstName, string lastName, string email, string password, int roleId, int? departmentId)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            RoleId = roleId,
            DepartmentId = departmentId,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = HashPassword(user, password);
        return user;
    }

    // helper for hashing passwords
    private static string HashPassword(User user, string password)
    {
        var hasher = new PasswordHasher<User>();
        return hasher.HashPassword(user, password);
    }
}