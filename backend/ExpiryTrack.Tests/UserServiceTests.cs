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
        var result = await service.SetActiveAsync(admin.Id, admin.Id, new SetUserActiveRequest { IsActive = false });

        Assert.NotNull(result.Conflict);
        Assert.Contains("cannot deactivate your own account.", result.Conflict);
    }

    // cannot deactivate the only finance officer
    [Fact]
    public async Task Deactivate_LastFinance_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", RoleId = 4 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        TestData.AddRequest(db, item, RequestStatus.PendingFinancialReview);

        db.Users.Add(admin);
        db.Users.Add(finance);
        db.SaveChanges();

        var service = new UserService(db);
        var result = await service.SetActiveAsync(finance.Id, admin.Id, new SetUserActiveRequest { IsActive = false });

        Assert.NotNull(result.Conflict);
        Assert.Contains("This is the only active finance officer and some requests are waiting for financial review.", result.Conflict);
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
        var result = await service.SetActiveAsync(employee.Id, admin.Id, new SetUserActiveRequest { IsActive = false });

        Assert.NotNull(result.Conflict);
        Assert.Contains("is still responsible for items", result.Conflict);
    }

    // unknown role
    [Fact]
    public async Task Create_UnknownRole_Error()
    {
        using var db = TestData.CreateDb();
        var request = new CreateUserRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@test.com",
            Password = "User123!",
            Role = "User",
            DepartmentId = null
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown role", result.Error);
        Assert.Null(result.User);
    }

    // employee without department
    [Fact]
    public async Task Create_Employee_WithoutDepartment_Error()
    {
        using var db = TestData.CreateDb();
        var request = new CreateUserRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@test.com",
            Password = "User123!",
            Role = "Employee",
            DepartmentId = null
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);

        Assert.NotNull(result.Error);
        Assert.Contains("must belong to a department", result.Error);
        Assert.Null(result.User);
    }

    // admin with a department 
    // unknown role
    [Fact]
    public async Task Create_AdminWithDepartment_Error()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var request = new CreateUserRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@test.com",
            Password = "User123!",
            Role = "Administrator",
            DepartmentId = item.DepartmentId,
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);

        Assert.NotNull(result.Error);
        Assert.Contains("do not belong to a department", result.Error);
        Assert.Null(result.User);
    }

    // admin without department
    [Fact]
    public async Task Create_AdminWithoutDepartment()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var request = new CreateUserRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@test.com",
            Password = "User123!",
            Role = "Administrator",
            DepartmentId = null,
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);

        Assert.Null(result.Error);
        Assert.NotNull(result.User);
    }

    // update async tests
    [Fact]
    public async Task Update_Valid_ChangesNames()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var request = new UpdateUserRequest()
        {
            FirstName = "  Ana   ",
            LastName = "Employee",
            Email = item.ResponsibleUser.Email,
        };

        var service = new UserService(db);
        var result = await service.UpdateAsync(item.ResponsibleUserId, request);

        Assert.Null(result.Conflict);
        Assert.Equal("Ana", result.User!.FirstName);
    }

    // emails conflict
    [Fact]
    public async Task Update_EmailOfAnotherUser_Conflict()
    {
        using var db = TestData.CreateDb();
        var item1 = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100);
        var item2 = TestData.AddItem(db, LifecycleState.Active, daysLeft: 800);
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        var admin2 = new User { FirstName = "Test", LastName = "Admin2", Email = "admin2@test.com", RoleId = 1 };

        db.Users.Add(admin);
        db.Users.Add(admin2);

        db.SaveChanges();

        var request = new UpdateUserRequest
        {
            Email = "admin@test.com"
        };

        var service = new UserService(db);
        var result = await service.UpdateAsync(item2.ResponsibleUserId, request);


        Assert.NotNull(result.Conflict);
        Assert.Contains("already exists", result.Conflict);
    }

    [Fact]
    public async Task SetActive_UserWithOpenRequest_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        db.Users.Add(admin);
        db.SaveChanges();

        var item = TestData.AddItem(db, LifecycleState.Expired, daysLeft: -5);
        TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);

        var request = new SetUserActiveRequest
        {
            IsActive = false
        };

        var service = new UserService(db);
        var result = await service.SetActiveAsync(item.ResponsibleUserId, admin.Id, request);

        Assert.NotNull(result.Conflict);
        Assert.Contains("open renewal requests", result.Conflict);
    }

    [Fact]
    public async Task SetActive_DepartmentManager_Conflict()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        db.Users.Add(admin);

        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };

        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        var employee = item.ResponsibleUser;

        var request = new SetUserActiveRequest
        {
            IsActive = false,
        };

        var service = new UserService(db);
        var result = await service.SetActiveAsync(manager.Id, admin.Id, request);

        Assert.NotNull(result.Conflict);
        Assert.Contains("manages a department", result.Conflict);
    }

    [Fact]
    public async Task Create_SecondManagerInDepartment_Conflict()
    {
        using var db = TestData.CreateDb();
        int departmentId = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100).DepartmentId;
        TestData.AddUser(db, 3, departmentId);
        
        var request = new CreateUserRequest
        {
            FirstName = "Test",
            LastName = "Manager 2",
            Email = "manager2@test.com",
            Password = "Manager123!",
            Role = "DepartmentManager",
            DepartmentId = departmentId
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);

        Assert.NotNull(result.Conflict);
        Assert.Null(result.Error);
    }
    
    [Fact]
    public async Task Create_ManagerWhenOldOneInactive_Ok()
    {
        using var db = TestData.CreateDb();
        int departmentId = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100).DepartmentId;
        TestData.AddUser(db, 3, departmentId, isActive: false);
        
        var request = new CreateUserRequest
        {
            FirstName = "Test",
            LastName = "Manager 2",
            Email = "manager2@test.com",
            Password = "Manager123!",
            Role = "DepartmentManager",
            DepartmentId = departmentId
        };

        var service = new UserService(db);
        var result = await service.CreateAsync(request);

        Assert.Null(result.Conflict);
        Assert.NotNull(result.User);
    }
}