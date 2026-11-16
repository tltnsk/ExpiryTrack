using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class NotificationsControllerTests
{
    [Fact]
    public async Task Get_ReturnsOnlyOwn()
    {
        using var db = TestData.CreateDb();
        var userA = new User { FirstName = "User", LastName = "A", Email = "a@test.com", RoleId = 2 };
        var userB = new User { FirstName = "User", LastName = "B", Email = "b@test.com", RoleId = 2 };
        db.Users.AddRange(userA, userB);
        db.SaveChanges();

        var older = new Notification
        {
            Type = "ItemExpiringSoon", Message = "older", RecipientUserId = userA.Id,
            CreatedAt = new DateTime(2026, 11, 1)
        };
        
        var newer = new Notification
        {
            Type = "ItemExpired", Message = "newer", RecipientUserId = userA.Id,
            CreatedAt = new DateTime(2026, 11, 2)
        };
        
        var forB = new Notification {Type="ItemExpired", Message = "for B", RecipientUserId = userB.Id, CreatedAt = new DateTime(2026, 11, 3)};
        
        db.Notifications.AddRange(older, newer, forB);
        db.SaveChanges();
        
        var controller = new NotificationsController(db);
        TestData.SetUser(controller, userA.Id);

        var result = await controller.Get();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<NotificationResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal(newer.Id, list[0].Id);
        Assert.Equal(older.Id, list[1].Id);
    }

    [Fact]
    public async Task UpdateRead_SomeoneElses_NotFound()
    {   
        using var db = TestData.CreateDb();
        var userA = new User { FirstName = "User", LastName = "A", Email = "a@test.com", RoleId = 2 };
        var userB = new User { FirstName = "User", LastName = "B", Email = "b@test.com", RoleId = 2 };
        db.Users.AddRange(userA, userB);
        db.SaveChanges();
        var forB = new Notification {Type="ItemExpired", Message = "for B", RecipientUserId = userB.Id, CreatedAt = new DateTime(2026, 11, 3)};
        
        db.Notifications.Add(forB);
        db.SaveChanges();
        
        var controller = new NotificationsController(db);
        TestData.SetUser(controller, userA.Id);
        
        var result = await controller.UpdateRead(forB.Id);
        
        Assert.IsType<NotFoundResult>(result);
        Assert.False(db.Notifications.Find(forB.Id)!.IsRead);
    }

    [Fact]
    public async Task UpdateRead_Own_MarkedAsRead()
    {
        using var db = TestData.CreateDb();
        var userA = new User { FirstName = "User", LastName = "A", Email = "a@test.com", RoleId = 2 };
        db.Users.Add(userA);
        db.SaveChanges();
        
        var notification = new Notification
        {
            Type = "ItemExpiringSoon", Message = "older", RecipientUserId = userA.Id,
            CreatedAt = new DateTime(2026, 11, 1)
        };
        db.Notifications.Add(notification);
        db.SaveChanges();
        
        var controller = new NotificationsController(db);
        TestData.SetUser(controller, userA.Id);
        
        var result = await controller.UpdateRead(notification.Id);
        
        Assert.IsType<OkObjectResult>(result);
        Assert.True(db.Notifications.Find(notification.Id)!.IsRead);
    }
    
    [Fact]
    public async Task UpdateRead_Missing_NotFound()
    {
        using var db = TestData.CreateDb();
        var userA = new User { FirstName = "User", LastName = "A", Email = "a@test.com", RoleId = 2 };
        db.Users.Add(userA);
        db.SaveChanges();
        
        var controller = new NotificationsController(db);
        TestData.SetUser(controller, userA.Id);
        
        var result = await controller.UpdateRead(999);
        
        Assert.IsType<NotFoundResult>(result);
    }
}