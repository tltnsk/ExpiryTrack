using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Services;
using ExpiryTrack.Api.Models.Enums;

namespace PrivateExpiryTrack.Tests;

public class ExpirationServiceTests
{
    // fixed today so that tests don't depend on the real date
    private static readonly DateOnly Today = new DateOnly(2026, 11, 1);

    // every test gets its own new and empty in-memory database
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // adds one item with category, department, employee and current period
    private static Item AddItem(AppDbContext db, LifecycleState state, int daysLeft, int warningPeriodDays = 30, User? manager = null)
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
    private static RenewalRequest AddRequest(AppDbContext db, Item item, RequestStatus status)
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

    [Fact]
    public async Task ActiveItem_BecomesExpiringSoon()
    {
        // 30 days left in a category with a 30 day warning period 
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.Active, daysLeft: 30, warningPeriodDays: 30);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        Assert.Equal(LifecycleState.ExpiringSoon, item.LifecycleState);
        var history = Assert.Single(db.ItemStateHistory);
        Assert.Equal(LifecycleState.Active, history.FromState);
        Assert.Equal(LifecycleState.ExpiringSoon, history.ToState);
        Assert.Null(history.ChangedByUserId);
        Assert.Single(db.Notifications);
    }

    [Fact]
    public async Task ExpiringSoonItem_BecomesExpired()
    {
        // expiring soon item becomes expired after the expiration date 
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: -1, warningPeriodDays: 30);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        Assert.Equal(LifecycleState.Expired, item.LifecycleState);
        var history = Assert.Single(db.ItemStateHistory);
        Assert.Equal(LifecycleState.ExpiringSoon, history.FromState);
        Assert.Equal(LifecycleState.Expired, history.ToState);
        Assert.Null(history.ChangedByUserId);
    }

    [Fact]
    public async Task ActiveItem_BecomesExpired()
    {
        // active item directly becomes expired after its expiration date
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.Active, daysLeft: -1, warningPeriodDays: 30);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        Assert.Equal(LifecycleState.Expired, item.LifecycleState);
        var history = Assert.Single(db.ItemStateHistory);
        Assert.Equal(LifecycleState.Active, history.FromState);
        Assert.Equal(LifecycleState.Expired, history.ToState);
        Assert.Null(history.ChangedByUserId);
    }

    [Fact]
    public async Task ExpiringSoonItem_OnExpirationDay()
    {
        // item that's expiring soon on its expiration day is still expiring soon
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 0, warningPeriodDays: 30);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        Assert.Equal(LifecycleState.ExpiringSoon, item.LifecycleState);
        Assert.Empty(db.ItemStateHistory);
    }

    [Fact]
    public async Task ActiveItem_WithMoreDaysLeft()
    {
        // item with more days left until warning period is still active
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.Active, daysLeft: 31, warningPeriodDays: 30);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        Assert.Equal(LifecycleState.Active, item.LifecycleState);
        Assert.Empty(db.ItemStateHistory);
    }

    [Fact]
    public async Task RenewalRequestRejected()
    {
        // item with expiration date passed
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: -1, warningPeriodDays: 30);
        var request = AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        // request status is changed to rejected and the request status is closed by expiry
        Assert.Equal(RequestStatus.Rejected, request.Status);
        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.ClosedByExpiry, action.ActionType);
        Assert.Equal(RequestStatus.PendingManagerApproval, action.FromStatus);
        Assert.Equal(RequestStatus.Rejected, action.ToStatus);
        Assert.Equal(LifecycleState.Expired, item.LifecycleState);
        Assert.Null(action.ActorUserId);
    }

    [Fact]
    public async Task RenewalRequestApproved()
    {
        // item with expiration date passed, but renewal request approved
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: -1, warningPeriodDays: 30);
        var request = AddRequest(db, item, RequestStatus.Approved);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        // the request status remains approved, but the lifecycle state is changed to expired
        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Empty(db.RequestActions);
        Assert.Equal(LifecycleState.Expired, item.LifecycleState);
    }

    [Fact]
    public async Task ExpiringSoonItem_ManagerNotifiedOnce()
    {
        // 15 days left with a 30-day warning period, so half has passed (30 / 2 = 15)
        using var db = CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 1 };
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15, warningPeriodDays: 30, manager: manager);
        var service = new ExpirationService(db);

        // first run
        await service.RunCheckAsync(Today);

        // escalation sent to the manager
        var notification = Assert.Single(db.Notifications);
        Assert.Equal("Escalation", notification.Type);
        Assert.Equal(manager.Id, notification.RecipientUserId);
        Assert.NotNull(item.EscalatedAt);

        // a second run must not notify the manager again
        await service.RunCheckAsync(Today);

        Assert.Single(db.Notifications);
    }

    [Fact]
    public async Task ExpiringSoonItem_OpenRequest_NoEscalation()
    {
        // half of the warning period has passed, but the employee already submitted a request
        using var db = CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 1 };
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15, warningPeriodDays: 30, manager: manager);
        AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        // the manager is not notified by the system and the item is not marked as escalated
        Assert.Empty(db.Notifications);
        Assert.Null(item.EscalatedAt);
    }

    [Fact]
    public async Task ExpiringSoonItem_NoManager_NoEscalation()
    {
        // half of the warning period has passed and there is no request since the department has no manager
        using var db = CreateDb();
        var item = AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15, warningPeriodDays: 30);
        var service = new ExpirationService(db);

        await service.RunCheckAsync(Today);

        // nobody to notifyn so notifications and EscalatedAt are empty 
        Assert.Empty(db.Notifications);
        Assert.Null(item.EscalatedAt);
    }
}