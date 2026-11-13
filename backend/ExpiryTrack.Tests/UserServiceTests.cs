using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class UserServiceTests
{
    // creating an employee in an existing department
    [Fact]
    public async Task Create_ValidEmployee()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);

        var request = new CreateUserRequest
        {
            FirstName = "New",
            LastName = "Employee",
            Email = "new@test.com",
            Password = "Employee123!",
            Role = "Employee",
            DepartmentId = item.DepartmentId,
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);
        
        Assert.Null(result.Error);
        Assert.Null(result.Conflict);
        
        Assert.NotNull(result.User);
        Assert.Equal("new@test.com", result.User.Email);
        Assert.Equal(item.DepartmentId, result.User.DepartmentId);
        Assert.True(result.User.IsActive);
        
        Assert.NotEqual("Employee123!", result.User.PasswordHash);
    }
    
    // Creating a user with an email that's already used
    [Fact]
    public async Task Create_DuplicateEmail_Conflict()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        
        string usedEmail = item.ResponsibleUser.Email;

        var request = new CreateUserRequest
        {
            FirstName = "New",
            LastName = "Employee",
            Email = usedEmail,
            Password = "Employee123!",
            Role = "Employee",
            DepartmentId = item.DepartmentId,
        };
        var service = new UserService(db);
        var result = await service.CreateAsync(request);
        
        Assert.NotNull(result.Conflict);
        Assert.Contains("email exists already", result.Conflict);
    }
    
    // admin can't deactivate own account
    [Fact]
    public async Task Deactivate_Admin_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        db.Users.Add(admin);
        db.SaveChanges();
        
        var service = new UserService(db);
        var result = await service.SetActiveAsync(admin.Id, admin.Id, new SetUserActiveRequest{IsActive = false});
        
        Assert.NotNull(result.Conflict);
        Assert.Contains("cannot deactivate your own account.", result.Conflict);
    }
    
    // an employee who's still responsible for an item cannot be deactivated
    [Fact]
    public async Task SetActive_UserWithItems_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        db.Users.Add(admin);
        db.SaveChanges();

        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var employee = item.ResponsibleUser;
        
        var service = new UserService(db);
        var result = await service.SetActiveAsync(employee.Id, admin.Id, new SetUserActiveRequest{IsActive = false});
        
        Assert.NotNull(result.Conflict);
        Assert.Contains("is still responsible for items", result.Conflict);
    }
}