using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class UsersControllerTests
{
    private static UsersController Makeontroller(Api.Data.AppDbContext db, int currentUserId)
    {
        var controller = new UsersController(db, new UserService(db));
        TestData.SetUser(controller, currentUserId);
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsUsersSortedByLastName()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        db.Users.Add(new User {FirstName = "Zara", LastName = "Aaronson", Email = "zara@test.com", RoleId = 4});
        db.SaveChanges();
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.GetAll();
        
        var ok =  Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsType<List<UserResponse>>(ok.Value);
        Assert.Equal(2, users.Count);
        Assert.Equal("Aaronson", users[0].LastName);
        Assert.Equal("FinanceOfficer", users[0].Role);
    }

    [Fact]
    public async Task GetById_ExistingUser_Ok()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.GetById(admin.Id);
        
        var ok =  Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal(admin.Email, user.Email);
    }
    
    [Fact]
    public async Task GetById_MissingUser_NotFound()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.GetById(999);
        
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ValidFinanceOfficer_Created()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.Create(new CreateUserRequest
        {
            FirstName = "Mila", LastName = "Mileva", Email = "mila@test.com", Password = "password1", Role = "FinanceOfficer",
        });
        
        var created = Assert.IsType<CreatedResult>(result);
        var user = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal("Mila", user.FirstName);
        Assert.Equal($"/api/users/{user.Id}", created.Location);
    }
    
    [Fact]
    public async Task Create_ExistingEmail_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.Create(new CreateUserRequest
        {
            FirstName = "A", LastName = "B", Email = admin.Email, Password = "password1", Role = "FinanceOfficer",
        });
        
        Assert.IsType<ConflictObjectResult>(result);
    }
    
    [Fact]
    public async Task Create_UnknownRole_BadRequest()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.Create(new CreateUserRequest
        {
            FirstName = "A", LastName = "B", Email = "new@test.com", Password = "password1", Role = "Boss",
        });
        
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unknown role.", bad.Value);
    }

    [Fact]
    public async Task Update_ValidDate_Ok()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1);
        var other = TestData.AddUser(db, roleId: 4);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.Update(other.Id, new UpdateUserRequest { FirstName = "New", LastName = "Name", Email = "new@test.com"});
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("new@test.com", user.Email);
    }
    
    [Fact]
    public async Task Update_MissingUser_NotFound()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1); 
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.Update(999, new UpdateUserRequest { FirstName = "New", LastName = "Name", Email = "new@test.com"});
        
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task Update_EmailOfAnotherUser_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1); 
        var other = TestData.AddUser(db, roleId: 4);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.Update(other.Id, new UpdateUserRequest { FirstName = "New", LastName = "Name", Email = admin.Email});
        
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task SetActive_DeactivateOtherUser_Ok()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1); 
        var other = TestData.AddUser(db, roleId: 4);
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.SetActive(other.Id, new SetUserActiveRequest { IsActive = false});
        
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.False(Assert.IsType<UserResponse>(ok.Value).IsActive);
    }
    
    [Fact]
    public async Task SetActive_DeactivateYourself_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1); 
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.SetActive(admin.Id, new SetUserActiveRequest { IsActive = false});
        
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("You cannot deactivate your own account.", conflict.Value);
    }
    
    [Fact]
    public async Task SetActive_MissingUser_NotFound()
    {
        using var db = TestData.CreateDb();
        var admin = TestData.AddUser(db, roleId: 1); 
        var controller = Makeontroller(db, admin.Id);
        
        var result = await controller.SetActive(999, new SetUserActiveRequest { IsActive = false});
        
        Assert.IsType<NotFoundResult>(result);
    }
}