using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class ItemsControllerTests
{
    [Fact]
    public async Task GetById_ResponsibleEmployee_Ok()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, item.ResponsibleUserId);
        
        var result = await controller.GetById(item.Id);
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ItemResponse>(ok.Value);
        Assert.Equal(item.Id, response.Id);
    }

    [Fact]
    public async Task GetById_OtherEmployee_NotFound()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var otherItem = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, otherItem.ResponsibleUserId);
        var result = await controller.GetById(item.Id);
        
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ManagerOfDepartment_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };

        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, manager.Id);
        
        var result = await controller.GetById(item.Id);
        
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task GetById_ManagerOfOtherDepartment_NotFound()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var otherManager = new User { FirstName = "Test", LastName = "Other Manager", Email = "othermanager@test.com", RoleId = 3 };

        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: otherManager);
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, otherManager.Id);
        
        var result = await controller.GetById(item.Id);
        
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task GetById_FinanceItemWithoutCost_NotFound()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", RoleId = 4 };
        db.Users.Add(finance);
        db.SaveChanges();
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, finance.Id);
        
        var result = await controller.GetById(item.Id);
        
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_FinanceItemWithCost_OK()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        item.CurrentPeriod!.Cost = 500;

        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", RoleId = 4 };
        db.Users.Add(finance);
        db.SaveChanges();
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, finance.Id);
        
        var result = await controller.GetById(item.Id);
        
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Admin_Ok()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        
        db.Users.Add(admin);
        db.SaveChanges();
        
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, admin.Id);
        
        var result = await controller.GetById(item.Id);
        
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task GetById_MissingItem_NotFound()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        
        db.Users.Add(admin);
        db.SaveChanges();
        
        var controller = new ItemsController(db, new ItemService(db));
        TestData.SetUser(controller, admin.Id);
        
        var result = await controller.GetById(99);
        
        Assert.IsType<NotFoundResult>(result);
    }
}