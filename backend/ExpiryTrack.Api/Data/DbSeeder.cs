using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(AppDbContext db)
    {
        // if the roles are already seeded
        if (await db.Roles.AnyAsync())
            return;

        db.Roles.AddRange(
            new Role { Name = "Administrator" },
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

    public static async Task SeedTestCategoriesAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync())
            return;

        db.Categories.AddRange(
        new Category
        {
            Name = "Software Licence",
            Description = "Software licences and subscriptions",
            WarningPeriodDays = 30
        },
        new Category
        {
            Name = "Contract",
            Description = "Service and supplier contracts",
            WarningPeriodDays = 60,
            RequiresFinancialReview = true,
            FinanceReviewThreshold = 5000
        },
        new Category
        {
            Name = "Insurance",
            Description = "Insurance policies",
            WarningPeriodDays = 45,
            RequiresFinancialReview = true
        });
        await db.SaveChangesAsync();
    }

    public static async Task SeedItemsAsync(AppDbContext db)
    {
        if (await db.Items.AnyAsync())
            return;

        var categories = await db.Categories.ToDictionaryAsync(c => c.Name);
        var employee = await db.Users.SingleAsync(u => u.Email == "employee@test.com");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await CreateItem(db, "SLL certificate for company website", "Sectigo", categories["Software Licence"], employee, today.AddDays(15), 89.90m);
        await CreateItem(db, "Microsoft 365 Business Standard", "Microsoft", categories["Software Licence"], employee, today.AddDays(21), 2400m);
        await CreateItem(db, "Office cleaning services contract", "CleanPro Services Ltd.", categories["Contract"], employee, today.AddDays(51), 9600m);
        await CreateItem(db, "Server room equipment insurance", "SafeGuard Insurance", categories["Insurance"], employee, today.AddDays(96), 1850m);
        await CreateItem(db, "JetBrains All Products Pack", "JetBrains", categories["Software Licence"], employee, today.AddDays(141), 780m);
        await CreateItem(db, "Dell hardware support agreement", "Dell", categories["Contract"], employee, today.AddDays(900), null);
    }
    private static async Task CreateItem(AppDbContext db, string name, string provider, Category category, User responsible, DateOnly expirationDate, decimal? cost)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        int daysLeft = expirationDate.DayNumber - today.DayNumber;
        var state = daysLeft <= category.WarningPeriodDays ? LifecycleState.ExpiringSoon : LifecycleState.Active;

        var item = new Item
        {
            Name = name,
            Provider = provider,
            CategoryId = category.Id,
            DepartmentId = responsible.DepartmentId!.Value,
            ResponsibleUserId = responsible.Id,
            LifecycleState = state,
            StateChangedAt = now,
            CreatedAt = now
        };

        var period = new LifecyclePeriod
        {
            PeriodNumber = 1,
            StartDate = expirationDate.AddYears(-1),
            ExpirationDate = expirationDate,
            Cost = cost,
            CreatedAt = now
        };

        item.Periods.Add(period);
        item.StateHistory.Add(new ItemStateHistory { FromState = null, ToState = state, Reason = "Item created", ChangedAt = now });
        db.Items.Add(item);
        await db.SaveChangesAsync();

        item.CurrentPeriodId = period.Id;
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