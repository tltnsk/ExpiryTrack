using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class ReportServiceTests
{
    // move the expiration date to the real date
    private static void SetDaysLeft(AppDbContext db, Item item, int daysLeft)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        item.CurrentPeriod!.ExpirationDate = today.AddDays(daysLeft);
        db.SaveChanges();
    }

    [Fact]
    public async Task Report_ExpiringItems_CorrectRanges()
    {
        using var db = TestData.CreateDb();
        var admin = new User {FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1};
        db.Users.Add(admin);
        db.SaveChanges();

        var within30 = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 10);
        var within60 = TestData.AddItem(db, LifecycleState.Active, daysLeft: 45);
        var within90 = TestData.AddItem(db, LifecycleState.Active, daysLeft: 80);
        
        var later = TestData.AddItem(db, LifecycleState.Active, daysLeft: 120);
        var cancelled = TestData.AddItem(db, LifecycleState.Cancelled, daysLeft: 20);
        
        SetDaysLeft(db, within30, 10);
        SetDaysLeft(db, within60, 45);
        SetDaysLeft(db, within90, 80);
        SetDaysLeft(db, later, 120);
        SetDaysLeft(db, cancelled, 20);

        within30.Provider = "Microsoft";
        within60.Provider = "Microsoft";
        within90.Provider = "Adobe";
        db.SaveChanges();

        var service = new ReportService(db);
        var result = await service.GetReportAsync(admin.Id);
        
        Assert.Equal(1, result.Expiring.Within30Days);
        Assert.Equal(1, result.Expiring.Within60Days);
        Assert.Equal(1, result.Expiring.Within90Days);
        Assert.Equal(4, result.Categories.Count);
        Assert.Equal("Microsoft", result.TopProviders[0].Name);
    }
    
    
    // manager can only see items from own department
    [Fact]
    public async Task Report_Manager_OnlyOwnDepartment()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3};
        var ownItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 10, manager: manager);
        var otherDepartmentItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15);
        
        SetDaysLeft(db, ownItem, 10);
        SetDaysLeft(db, otherDepartmentItem, 15);
        
        var service = new ReportService(db);
        var result = await service.GetReportAsync(manager.Id);
        
        Assert.Equal(1, result.Expiring.Within30Days);
    }
    
    // finance report counts items that have a cost
    [Fact]
    public async Task Report_Finance_OnlyOwnDepartment()
    {
        using var db = TestData.CreateDb();
        var finance = new User {FirstName = "Test", LastName = "Finance", Email = "finance@test.com", RoleId = 4};
        db.Users.Add(finance);
        db.SaveChanges();

        var withCost = TestData.AddItem(db, LifecycleState.Active, daysLeft: 45);
        var withoutCost = TestData.AddItem(db, LifecycleState.Active, daysLeft: 50);
        
        SetDaysLeft(db, withCost, 45);
        SetDaysLeft(db, withoutCost, 50);
        withCost.CurrentPeriod!.Cost = 1000;
        db.SaveChanges();
        
        var service = new ReportService(db);
        var result = await service.GetReportAsync(finance.Id);
        
        Assert.Equal(1, result.Expiring.Within60Days);
    }
}