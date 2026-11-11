using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // look up role ids by name
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);
        
        // load departments from database
        var departments = await db.Departments.ToDictionaryAsync(d => d.Name);

        int admin = roles["Administrator"];
        int manager = roles["DepartmentManager"];
        int employee = roles["Employee"];
        int finance = roles["FinanceOfficer"];

        // users without a department
        var adminUser = CreateUser("Test", "Admin", "admin@test.com", "Admin123!", admin, null);
        var financeUser = CreateUser("Test", "Finance", "finance@test.com", "Finance123!", finance, null);

        // IT 
        var itManager = CreateUser("Test", "Manager", "manager@test.com", "Manager123!", manager, departments["IT"].Id);
        var itEmployee = CreateUser("Test", "Employee", "employee@test.com", "Employee123!", employee, departments["IT"].Id);
        var itEmployee2 = CreateUser("John", "Brown", "john.brown@test.com", "Employee123!", employee, departments["IT"].Id);

        // Marketing
        var marketingManager = CreateUser("Rachel", "Williams", "rachel.williams@test.com", "Manager123!", manager, departments["Marketing"].Id);
        var marketingEmployee1 = CreateUser("Anna", "Peterson", "anna.peterson@test.com", "Employee123!", employee, departments["Marketing"].Id);
        var marketingEmployee2 = CreateUser("Marc", "Anderson", "marc.anderson@test.com", "Employee123!", employee, departments["Marketing"].Id);

        // Sales
        var salesManager = CreateUser("Andrew", "Cope", "andrew.cope@test.com", "Manager123!", manager, departments["Sales"].Id);
        var salesEmployee1 = CreateUser("Alex", "Turner", "alex.turner@test.com", "Employee123!", employee, departments["Sales"].Id);

        // Operations
        var operationsManager = CreateUser("Matt", "Davis", "matt.davis@test.com", "Manager123!", manager, departments["Operations"].Id);
        var operationsEmployee1 = CreateUser("Sophie", "Clark", "sophie.clark@test.com", "Employee123!", employee, departments["Operations"].Id);

        // HR 
        var hrManager = CreateUser("Kate", "Taylor", "kate.taylor@test.com", "Manager123!", manager, departments["HR"].Id);
        var hrEmployee1 = CreateUser("Emily", "Moore", "emily.moore@test.com", "Employee123!", employee, departments["HR"].Id);

        db.Users.AddRange(
            adminUser, financeUser,
            itManager, itEmployee, itEmployee2,
            marketingManager, marketingEmployee1, marketingEmployee2,
            salesManager, salesEmployee1,
            operationsManager, operationsEmployee1,
            hrManager, hrEmployee1);
        await db.SaveChangesAsync();

        // the managers have Ids only after being saved
        departments["IT"].ManagerId = itManager.Id;
        departments["Marketing"].ManagerId = marketingManager.Id;
        departments["Sales"].ManagerId = salesManager.Id;
        departments["Operations"].ManagerId = operationsManager.Id;
        departments["HR"].ManagerId = hrManager.Id;
        await db.SaveChangesAsync();
    }

    private static User CreateUser(string firstName, string lastName, string email, string password, int roleId, int? departmentId)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            RoleId = roleId,
            DepartmentId = departmentId,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = HashPassword(user, password);
        return user;
    }

    private static string HashPassword(User user, string password)
    {
        var hasher = new PasswordHasher<User>();
        return hasher.HashPassword(user, password);
    }
}