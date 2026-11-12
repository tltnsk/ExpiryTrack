using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class ItemServiceTests
{
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
}