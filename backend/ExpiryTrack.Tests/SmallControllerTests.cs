using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class SmallControllerTests
{
    private static DashboardController MakeDashboard(Api.Data.AppDbContext db, int userId)
    {
        var controller = new DashboardController(new DashboardService(db));
        TestData.SetUser(controller, userId);
        return controller;
    }

    [Fact]
    public async Task Dashboard_Admin_CountsAllItems()
    {
        using var db = TestData.CreateDb();
        TestData.AddItem(db, LifecycleState.Active, 100);
        TestData.AddItem(db, LifecycleState.Expired, -5);
        var admin = TestData.AddUser(db, roleId: 1);

        var result = await MakeDashboard(db, admin.Id).GetAdminDashboard();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var dashboard = Assert.IsType<AdminDashboardResponse>(ok.Value);
        Assert.Equal(2, dashboard.TotalItems);
        Assert.Equal(1, dashboard.ExpiredItems);
    }

    [Fact]
    public async Task Dashboard_Manager_OnlyOwnDepartment()
    {
        using var db = TestData.CreateDb();
        var manager = new User {FirstName = "Maria", LastName = "Jones",  Email = "maria@test.com", RoleId = 3};
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, 10, manager: manager);
        TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        TestData.AddItem(db, LifecycleState.Active, 100);
        
        var result = await MakeDashboard(db, manager.Id).GetManagerDashboard();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var dashboard = Assert.IsType<ManagerDashboardResponse>(ok.Value);
        Assert.Equal(1, dashboard.TotalItems);
        Assert.Single(dashboard.RequestsToReview);
    }
    
    [Fact]
    public async Task Dashboard_Employee_OnlyOwnItems()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, 10);
        TestData.AddItem(db, LifecycleState.Active, 100);
        
        var result = await MakeDashboard(db, item.ResponsibleUserId).GetEmployeeDashboard();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var dashboard = Assert.IsType<EmployeeDashboardResponse>(ok.Value);
        Assert.Equal(1, dashboard.TotalItems);
        Assert.Single(dashboard.ItemsNeedingRenewal);
    }
    
    [Fact]
    public async Task Dashboard_Finance_Ok()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, 10);
        item.CurrentPeriod!.Cost = 250;
        db.SaveChanges();
        var finance = TestData.AddUser(db, roleId: 4);
        
        var result = await MakeDashboard(db, finance.Id).GetFinanceDashboard();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var dashboard = Assert.IsType<FinanceDashboardResponse>(ok.Value);
        Assert.Equal(250, dashboard.ExpectedUpcomingCosts);
    }
}