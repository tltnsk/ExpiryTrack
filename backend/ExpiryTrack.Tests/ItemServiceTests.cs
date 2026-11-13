using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class ItemServiceTests
{
    // Cancelling items tests
    [Fact]
    public async Task Cancel_Active_Item()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 70, manager: manager);

        var service = new ItemService(db);
        
        var result = await service.CancelAsync(item.Id, manager.Id);
        
        Assert.Null(result.Error);
        Assert.False(result.NotFound);
        
        Assert.NotNull(result.Item);
        Assert.Equal(LifecycleState.Cancelled, result.Item.LifecycleState);
        
        // the change is recorded in history
        var history = Assert.Single(db.ItemStateHistory);
        Assert.Equal(LifecycleState.Active, history.FromState);
        Assert.Equal(LifecycleState.Cancelled, history.ToState);
        Assert.Equal(manager.Id, history.ChangedByUserId);
        
        // responsible employee is notified
        var notification = Assert.Single(db.Notifications);
        Assert.Equal("ItemCancelled", notification.Type);
        Assert.Equal(item.ResponsibleUserId, notification.RecipientUserId);
    }
    
    [Fact]
    public async Task Cancel_ExpiredItem_Refused()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Expired, daysLeft: -5, manager: manager);

        var service = new ItemService(db);
        
        var result = await service.CancelAsync(item.Id, manager.Id);
        
        Assert.NotNull(result.Error);
        Assert.Contains("Only Active and Expiring Soon", result.Error);

        Assert.Equal(LifecycleState.Expired, item.LifecycleState);
        Assert.Empty(db.ItemStateHistory);
        Assert.Empty(db.Notifications);
    }
    
    [Fact]
    public async Task Cancel_OpenRequest_Refused()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 10, manager: manager);
        
        TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        
        var service = new ItemService(db);
        
        var result = await service.CancelAsync(item.Id, manager.Id);
        
        Assert.NotNull(result.Error);
        Assert.Contains("renewal request open", result.Error);

        Assert.Equal(LifecycleState.ExpiringSoon, item.LifecycleState);
        Assert.Empty(db.ItemStateHistory);
        Assert.Empty(db.Notifications);
    }
    
    // Reassigning items
    
    // Reassigning an active item to an employee of own department 
    [Fact]
    public async Task Reassign_Item_Valid()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 105, manager: manager);
        var newEmployee = new User { FirstName = "Responsible", LastName = "Employee", Email = "responsible@test.com", RoleId = 2, DepartmentId = item.DepartmentId };
        db.Users.Add(newEmployee);
        db.SaveChanges();

        int oldEmployeeId = item.ResponsibleUserId;
        var service = new ItemService(db);
        var result = await service.ReassignAsync(item.Id, manager.Id,
            new ReassignItemRequest { ResponsibleUserId = newEmployee.Id });
        
        Assert.Null(result.Error);
        Assert.False(result.NotFound);
        
        Assert.Equal(item.ResponsibleUserId, newEmployee.Id);

        // both employees are notified
        Assert.Equal(2, db.Notifications.Count());
        Assert.Contains(db.Notifications, n => n.RecipientUserId == newEmployee.Id);
        Assert.Contains(db.Notifications, n => n.RecipientUserId == oldEmployeeId);
    }
    
    // Reassigning an item to an employee from different department should result in an error
    [Fact]
    public async Task Reassign_Item_Different_Department()
    {
        using var db = TestData.CreateDb();
        var managerA = new User { FirstName = "TestA", LastName = "Manager", Email = "managera@test.com", RoleId = 3 };
        var managerB = new User { FirstName = "TestB", LastName = "Manager", Email = "managerb@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 105, manager: managerA);
        var otherItem = TestData.AddItem(db, LifecycleState.Active, daysLeft: 105, manager: managerB);
        
        int oldEmployeeId = item.ResponsibleUserId;
        var service = new ItemService(db);
        var result = await service.ReassignAsync(item.Id, managerA.Id,
            new ReassignItemRequest { ResponsibleUserId = otherItem.ResponsibleUserId });
        
        Assert.NotNull(result.Error);
        Assert.Contains("does not belong to your department", result.Error);

        Assert.Equal(item.ResponsibleUserId, oldEmployeeId);
        Assert.Empty(db.Notifications);
    }
    
    // reassigning an item to a user who is not an employee
    [Fact]
    public async Task Reassign_Item_To_NonEmployee()
    {
        using var db = TestData.CreateDb();
        var managerA = new User { FirstName = "TestA", LastName = "Manager", Email = "managera@test.com", RoleId = 3 };
        var managerB = new User { FirstName = "TestB", LastName = "Manager", Email = "managerb@test.com", RoleId = 3 };
        db.Users.Add(managerB);
        db.SaveChanges();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 105, manager: managerA);

        int oldEmployeeId = item.ResponsibleUserId;
        var service = new ItemService(db);
        var result = await service.ReassignAsync(item.Id, managerA.Id,
            new ReassignItemRequest { ResponsibleUserId = managerB.Id });
        
        Assert.NotNull(result.Error);
        Assert.Contains("is not an employee", result.Error);

        Assert.Equal(item.ResponsibleUserId, oldEmployeeId);
        Assert.Empty(db.Notifications);
    }
    
    // reassigning an item to a user who is not in the database
    [Fact]
    public async Task Reassign_Item_User_Not_Found()
    {
        using var db = TestData.CreateDb();
        var managerA = new User { FirstName = "TestA", LastName = "Manager", Email = "managera@test.com", RoleId = 3 };
        
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 105, manager: managerA);

        int oldEmployeeId = item.ResponsibleUserId;
        var service = new ItemService(db);
        var result = await service.ReassignAsync(item.Id, managerA.Id,
            new ReassignItemRequest { ResponsibleUserId = 999 });
        
        Assert.NotNull(result.Error);
        Assert.Contains("User not found", result.Error);

        Assert.Equal(item.ResponsibleUserId, oldEmployeeId);
        Assert.Empty(db.Notifications);
    }
    
    // item already assigned to this employee
    [Fact]
    public async Task Reassign_Item_To_Same_Employee()
    {
        using var db = TestData.CreateDb();
        var managerA = new User { FirstName = "TestA", LastName = "Manager", Email = "managera@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 105, manager: managerA);

        int oldEmployeeId = item.ResponsibleUserId;
        var service = new ItemService(db);
        var result = await service.ReassignAsync(item.Id, managerA.Id,
            new ReassignItemRequest { ResponsibleUserId = oldEmployeeId });
        
        Assert.NotNull(result.Error);
        Assert.Contains("already assigned to this employee", result.Error);

        Assert.Equal(item.ResponsibleUserId, oldEmployeeId);
        Assert.Empty(db.Notifications);
    }
    
    // Creating an item
    // creating a valid item
    [Fact]
    public async Task Create_ValidItem_Created()
    {
        using var db = TestData.CreateDb();
        var existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new CreateItemRequest
        {
            Name = "Test Item",
            CategoryId = existing.CategoryId,
            StartDate = today,
            ExpirationDate = today.AddDays(100),
        };
        var service = new ItemService(db);
        var result = await service.CreateAsync(existing.ResponsibleUserId, request);
        
        Assert.Null(result.Error);
        Assert.Equal(LifecycleState.Active, result.Item!.LifecycleState);
        Assert.Equal(result.Item.ResponsibleUserId, existing.ResponsibleUserId);
        Assert.NotNull(result.Item.CurrentPeriodId);
        var history = db.ItemStateHistory.Single(h => h.ItemId == result.Item.Id);
        Assert.Equal("Item created", history.Reason);
    }
    
    // creating a valid item with 10 days left to expiration
    // lifecycle state must be expiring soon 
    [Fact]
    public async Task Create_WithinWarningPeriod_ExpiringSoon()
    {
        using var db = TestData.CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 10);

        var request = new CreateItemRequest
        {
            Name = "Test Item",
            CategoryId = existing.CategoryId,
            StartDate = today,
            ExpirationDate = today.AddDays(10),
        };
        var service = new ItemService(db);
        var result = await service.CreateAsync(existing.ResponsibleUserId, request);
        
        Assert.Null(result.Error);
        Assert.Equal(LifecycleState.ExpiringSoon, result.Item!.LifecycleState);
        Assert.Equal(existing.ResponsibleUserId, result.Item.ResponsibleUserId);
        Assert.NotNull(result.Item.CurrentPeriodId);
        var history = db.ItemStateHistory.Single(h => h.ItemId == result.Item.Id);
        Assert.Equal("Item created", history.Reason);
    }

    // creating an item with an invalid expiration date is refused
    [Fact]
    public async Task Create_PastExpirationDate_Refused()
    {
        using var db = TestData.CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = TestData.AddItem(db, LifecycleState.Active, daysLeft: 10);

        var request = new CreateItemRequest
        {
            Name = "Test Item",
            CategoryId = existing.CategoryId,
            StartDate = today,
            ExpirationDate = today.AddDays(-10),
        };
        var service = new ItemService(db);
        var result = await service.CreateAsync(existing.ResponsibleUserId, request);

        Assert.NotNull(result.Error);
        Assert.Contains("must be a future date", result.Error);
        Assert.Equal(1, db.Items.Count());
    }

    [Fact]
    public async Task Update_ActiveItem_Updated()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 70, manager: manager);
        

        var request = new UpdateItemRequest
        {
            Name = "  New name ",
            Provider = "New provider"
        };
        
        var service = new ItemService(db);
        var result = await service.UpdateAsync(item.Id, manager.Id, request);

        Assert.Null(result.Error);
        Assert.Equal("New name", item.Name);
        Assert.Equal("New provider", item.Provider);
    }
    
    
    [Fact]
    public async Task Update_ExpiredItem_Refused()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Expired, daysLeft: -20, manager: manager);

        var request = new UpdateItemRequest
        {
            Name = "  Test Item  ",
        };
        
        var service = new ItemService(db);
        var result = await service.UpdateAsync(item.Id, manager.Id, request);

        Assert.NotNull(result.Error);
        Assert.Contains("Only Active and Expiring Soon items can be updated.", result.Error);
    }
}