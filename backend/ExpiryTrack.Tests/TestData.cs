using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace ExpiryTrack.Tests;

public static class TestData
{
    // fixed today so that tests don't depend on the real date
    public static readonly DateOnly Today = new DateOnly(2026, 11, 1);

    // a new empty SQLite database in memory 
    public static AppDbContext CreateEmptyDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var db = new AppDbContext(options);
        // creates the tables with all the keys and constraints
        db.Database.EnsureCreated();
        return db;
    }
    public static AppDbContext CreateDb()
    {
        var db = CreateEmptyDb();
        // users need an existing role
        // because SQLite checks foreign keys
        db.Roles.AddRange(
            new Role {Id = 1, Name = "Administrator"},
            new Role {Id = 2, Name = "Employee"},
            new Role {Id = 3, Name = "DepartmentManager"},
            new Role {Id = 4, Name = "FinanceOfficer"});
        db.SaveChanges();
        
        return db;
    }
    
    // to make the name unique if the test adds more than one item
    private static int counter = 0;

    // adds one item with category, department, employee and current period
    public static Item AddItem(AppDbContext db, LifecycleState state, int daysLeft, int warningPeriodDays = 30, User? manager = null, bool requiresFinancialReview = false, decimal? threshold = null)
    {
        counter++;
        
        var department = new Department { Name = "IT" + counter };
        db.Departments.Add(department);
        db.SaveChanges();
        
        if (manager != null)
        {
            manager.DepartmentId = department.Id;
            db.Users.Add(manager);
            db.SaveChanges();
            department.ManagerId = manager.Id;
        }
        
        var employee = new User { FirstName = "Test", LastName = "Employee", Email = $"employee{counter}@test.com", RoleId = 2, DepartmentId = department.Id };
        var category = new Category
        {
            Name = "Software Licence" + counter,
            WarningPeriodDays = warningPeriodDays,
            RequiresFinancialReview = requiresFinancialReview,
            FinanceReviewThreshold = threshold,
        };

        var item = new Item
        {
            Name = "Test Item",
            LifecycleState = state,
            DepartmentId = department.Id,
            Category = category,
            ResponsibleUser = employee,
        };
        
        var expirationDate = Today.AddDays(daysLeft);
        var period = new LifecyclePeriod
        {
            Item = item,
            PeriodNumber = 1,
            StartDate = expirationDate.AddYears(-1),
            ExpirationDate = Today.AddDays(daysLeft),
        };

        db.Items.Add(item);
        db.LifecyclePeriods.Add(period);
        db.SaveChanges();
        
        // since both the item and the period exist
        // the item can now point to its current period
        item.CurrentPeriod = period;
        db.SaveChanges();
        
        return item;
    }

    // adds a renewal request
    public static RenewalRequest AddRequest(AppDbContext db, Item item, RequestStatus status)
    {
        var request = new RenewalRequest
        {
            PeriodId = item.CurrentPeriodId!.Value,
            RequestedByUserId = item.ResponsibleUserId,
            Status = status,
            ProposedExpirationDate = Today.AddYears(1),
            SubmittedAt = DateTime.UtcNow,
        };
        db.RenewalRequests.Add(request);
        db.SaveChanges();
        return request;
    }
    
    // adds a user with the given role 
    public static User AddUser(AppDbContext db, int roleId, int? departmentId = null, string password = "password1",
        bool isActive = true)
    {
        counter++;
        var user = new User
        {
            FirstName = "User",
            LastName = "Number" + counter,
            Email = $"user{counter}@test.com",
            RoleId = roleId,
            DepartmentId = departmentId,
            IsActive = isActive,
        };
        user.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static void SetUser(ControllerBase controller, int userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
