using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class DashboardServiceTests
{
    // the employee only sees their own items and requests
    [Fact]
    public async Task EmployeeDashboard_MixedItems_CorrectCounts()
    {
        using var db = TestData.CreateDb();
        var active = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var expiringSoon = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 10);
        var expired = TestData.AddItem(db, LifecycleState.Expired, daysLeft: -5);
        var approved = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var cancelled = TestData.AddItem(db, LifecycleState.Cancelled, daysLeft: 60);

        var otherEmployeeItem = TestData.AddItem(db, LifecycleState.Active, daysLeft: 50);

        // AddItem creates a new employee each time so give the items to one employee
        int employeeId = active.ResponsibleUserId;
        expiringSoon.ResponsibleUserId = employeeId;
        expired.ResponsibleUserId = employeeId;
        approved.ResponsibleUserId = employeeId;
        cancelled.ResponsibleUserId = employeeId;
        db.SaveChanges();
        
        TestData.AddRequest(db, approved, RequestStatus.Approved);

        var service = new DashboardService(db);
        var result = await service.GetEmployeeDashboardAsync(employeeId);

        Assert.Equal(1, result.ActiveItems);
        Assert.Equal(1, result.CancelledItems);
        Assert.Equal(1, result.ExpiredItems);
        Assert.Equal(2, result.ExpiringSoonItems);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(2, result.ItemsNeedingRenewal.Count);
        Assert.Single(result.MyOpenRequests);
        Assert.Empty(result.RequestsNeedingClarification);
        Assert.Single(result.RequestsAwaitingCompletion);
        Assert.Equal(2, result.UpcomingExpirations.Count);
        Assert.Equal(expiringSoon.Id, result.UpcomingExpirations[0].Id);
    }
    
    // the manager only sees their own department
    [Fact]
    public async Task ManagerDashboard_OwnDepartment_CorrectCounts()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var active = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        var waiting = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 10);
        var approved = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var expired = TestData.AddItem(db, LifecycleState.Expired, daysLeft: -5);

        var otherDepartmentItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 5);
        
        int departmentId = active.DepartmentId;
        waiting.DepartmentId = departmentId;
        approved.DepartmentId = departmentId;
        expired.DepartmentId = departmentId;
        db.SaveChanges();

        TestData.AddRequest(db, waiting, RequestStatus.PendingManagerApproval);
        TestData.AddRequest(db, approved, RequestStatus.Approved);
        TestData.AddRequest(db, otherDepartmentItem, RequestStatus.PendingManagerApproval);

        var service = new DashboardService(db);
        var result = await service.GetManagerDashboardAsync(manager.Id);
        
        Assert.Equal(1, result.ActiveItems);
        Assert.Equal(0, result.CancelledItems);
        Assert.Equal(1, result.ExpiredItems);
        Assert.Equal(2, result.ExpiringSoonItems);
        Assert.Equal(4, result.TotalItems);
        Assert.Single(result.RequestsAwaitingCompletion);
        Assert.Single(result.RequestsToReview);
        Assert.Equal(2, result.UpcomingExpirations.Count);
    }
    
    // the admin sees the items of all departments
    [Fact]
    public async Task AdminDashboard_AllDepartments_CorrectCounts()
    {
        using var db = TestData.CreateDb();
        var activeA = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var expiringA = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15);
        var expiringB = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 5);
        var expiredB = TestData.AddItem(db, LifecycleState.Expired, daysLeft: -5);
        var cancelledB = TestData.AddItem(db, LifecycleState.Cancelled, daysLeft: 60);

        // department A gets 2 items, department B gets 3
        expiringA.DepartmentId = activeA.DepartmentId;
        expiredB.DepartmentId = expiringB.DepartmentId;
        cancelledB.DepartmentId = expiringB.DepartmentId;
        db.SaveChanges();

        var service = new DashboardService(db);
        var result = await service.GetAdminDashboardAsync();

        // asserts
        Assert.Equal(1, result.ActiveItems);
        Assert.Equal(1, result.CancelledItems);
        Assert.Equal(1, result.ExpiredItems);
        Assert.Equal(2, result.ExpiringSoonItems);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(2, result.UpcomingExpirations.Count);
        Assert.Equal(expiringB.Id, result.UpcomingExpirations[0].Id);
        Assert.Contains(result.ItemsByDepartment, d => d.ItemCount == 3);
        Assert.Contains(result.ItemsByDepartment, d => d.ItemCount == 2);
    }
    
    // finance sees the requests waiting for review and the cost sums
    [Fact]
    public async Task FinanceDashboard_PendingRequests_CorrectCosts()
    {
        using var db = TestData.CreateDb();
        var first = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 10);
        var second = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var approvedItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 25);

        // it's active, so the cost is not an upcoming cost
        var activeItem = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);

        // the current costs of the items
        first.CurrentPeriod!.Cost = 1000;
        second.CurrentPeriod!.Cost = 500;
        approvedItem.CurrentPeriod!.Cost = 300;
        activeItem.CurrentPeriod!.Cost = 9999;
        db.SaveChanges();
        
        var firstRequest = TestData.AddRequest(db, first, RequestStatus.PendingFinancialReview);
        var secondRequest = TestData.AddRequest(db, second, RequestStatus.PendingFinancialReview);
        var approvedRequest = TestData.AddRequest(db, approvedItem, RequestStatus.Approved);
        firstRequest.ProposedCost = 1100;
        firstRequest.NeedsFinancialReview = true;
        secondRequest.ProposedCost = 600;
        secondRequest.NeedsFinancialReview = true;
        approvedRequest.ProposedCost = 400;
        approvedRequest.NeedsFinancialReview = true;
        db.SaveChanges();

        var service = new DashboardService(db);
        var result = await service.GetFinanceDashboardAsync();

        Assert.Equal(2, result.RequestsToReviewCount);
        Assert.Equal(1700, result.TotalCostWaitingForReview);
        Assert.Equal(1800, result.ExpectedUpcomingCosts);
        Assert.Equal(400, result.ApprovedCost);
        Assert.Equal(2, result.RequestsNeedingFinancialReview.Count);
    }
}