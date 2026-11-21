using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_WrongPassword_Unauthorized()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 2, password: "password123");
        var controller = new AuthController(db);
        
        var result = await controller.Login(new LoginRequest {Email = user.Email, Password = "wrongpassword"});
        
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password.", unauthorized.Value);
    }
    
    [Fact]
    public async Task Login_UnknownEmail_SameMessage()
    {
        using var db = TestData.CreateDb();
        var controller = new AuthController(db);
        
        var result = await controller.Login(new LoginRequest {Email = "email@test.com", Password = "password123"});
        
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password.", unauthorized.Value);
    }
    
    [Fact]
    public async Task Login_InactiveUser_Unauthorized()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 2, password: "password123", isActive: false);
        var controller = new AuthController(db);
        
        var result = await controller.Login(new LoginRequest {Email = user.Email, Password = "wrongpassword"});
        
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public async Task LoggedInUser_Ok()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 1);
        var controller = new AuthController(db);
        TestData.SetUser(controller, user.Id);

        var result = await controller.Me();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Administrator", Assert.IsType<UserResponse>(ok.Value).Role);
    }
    
    [Fact]
    public async Task DeactivatedUser_Unauthorized()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 1, isActive: false);
        var controller = new AuthController(db);
        TestData.SetUser(controller, user.Id);

        var result = await controller.Me();
        
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrent()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 2, password: "password123");
        
        var controller = new AuthController(db);
        TestData.SetUser(controller, user.Id);
        
        var result = await controller.ChangePassword(new ChangePasswordRequest{CurrentPassword = "password123", NewPassword = "newpassword"});
        
        Assert.IsType<NoContentResult>(result);
        var hasher = new PasswordHasher<User>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, "newpassword"));
    }
    
    [Fact]
    public async Task ChangePassword_WrongCurrent()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 2, password: "password123");
        var oldHash = user.PasswordHash;
        var controller = new AuthController(db);
        TestData.SetUser(controller, user.Id);
        
        var result = await controller.ChangePassword(new ChangePasswordRequest{CurrentPassword = "wrong", NewPassword = "newpassword"});
        
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid password.", bad.Value);
        Assert.Equal(oldHash, user.PasswordHash);
    }
    
    [Fact]
    public async Task ChangePassword_DeactivatedUser_Unauthorized()
    {
        using var db = TestData.CreateDb();
        var user = TestData.AddUser(db, roleId: 2, isActive: false);
        var controller = new AuthController(db);
        TestData.SetUser(controller, user.Id);
        
        var result = await controller.ChangePassword(new ChangePasswordRequest{CurrentPassword = "password1", NewPassword = "newpassword"});
        
        Assert.IsType<UnauthorizedResult>(result);
    }
}