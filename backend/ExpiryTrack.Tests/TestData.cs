using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Tests;

public static class TestData
{
    // fixed today so that tests don't depend on the real date
    public static readonly DateOnly Today = new DateOnly(2026, 11, 1);

    // every test gets its own new and empty in-memory database
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // adds one item with category, department, employee and current period
    public static Item AddItem(AppDbContext db, LifecycleState state, int daysLeft, int warningPeriodDays = 30, User? manager = null)
    {
        var department = new Department { Name = "IT" };
        if (manager != null)
        {
            manager.Department = department;
            department.Manager = manager;
        }
        var employee = new User { FirstName = "Test", LastName = "Employee", Email = "employee@test.com", RoleId = 1, Department = department };
        var category = new Category { Name = "Software Licence", WarningPeriodDays = warningPeriodDays };

        var item = new Item
        {
            Name = "Test Item",
            LifecycleState = state,
            Department = department,
            Category = category,
            ResponsibleUser = employee,
        };
        var period = new LifecyclePeriod
        {
            Item = item,
            PeriodNumber = 1,
            StartDate = Today.AddYears(-1),
            ExpirationDate = Today.AddDays(daysLeft),
        };
        item.CurrentPeriod = period;

        db.Items.Add(item);
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
}
