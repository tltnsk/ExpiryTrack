using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class DepartmentsControllerTests
{
    [Fact]
    public async Task GetById_Missing_NotFound()
    {
        using var db = TestData.CreateDb();
        var controller = new DepartmentsController(db);
        var result = await controller.GetById(999);
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFound);
    }

    [Fact]
    public async Task Create_DuplicateName_Conflict()
    {
        using var db = TestData.CreateDb();
        db.Departments.Add(new Department { Name = "IT" });
        db.SaveChanges();
        var controller = new DepartmentsController(db);

        var result = await controller.Create(new DepartmentRequest { Name = "  IT  ", IsActive = true });
        
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("A department with this name already exists.", conflict.Value);
        Assert.Single(db.Departments);
    }
    
    [Fact]
    public async Task Create_NewName_Created()
    {
        using var db = TestData.CreateDb();
        var controller = new DepartmentsController(db);

        var result = await controller.Create(new DepartmentRequest { Name = "HR", IsActive = true });
        
        var created = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<DepartmentResponse>(created.Value);
        Assert.Equal("HR", response.Name);
        Assert.Single(db.Departments);
    }

    [Fact]
    public async Task Update_MissingDepartment_NotFound()
    {
        using var db = TestData.CreateDb();
        var controller = new DepartmentsController(db);
        
        var result = await controller.Update(999, new DepartmentRequest { Name = "HR", IsActive = true });
        
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFound);
    }

    [Fact]
    public async Task Update_NameOfOtherDepartment_Conflict()
    {
        using var db = TestData.CreateDb();
        db.Departments.Add(new Department { Name = "IT" });
        Department hrDepartment = new Department { Name = "HR" };
        db.Departments.Add(hrDepartment);
        db.SaveChanges();
        var controller = new DepartmentsController(db);

        var result = await controller.Update(hrDepartment.Id, new DepartmentRequest { Name = "IT", IsActive = true });
        
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Name already exists.", conflict.Value);
        Assert.Equal("HR", hrDepartment.Name);
    }

    [Fact]
    public async Task AssignManager_ValidManager_Assigned()
    {
        using var db = TestData.CreateDb();
        var it = new Department { Name = "IT" };
        db.Departments.Add(it);
        db.SaveChanges();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3, DepartmentId = it.Id};
        db.Users.Add(manager);
        db.SaveChanges();
        var controller = new DepartmentsController(db);
        
        var result = await controller.AssignManager(it.Id, new AssignManagerRequest { UserId = manager.Id });
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DepartmentResponse>(ok.Value);
        Assert.Equal(manager.Id, response.ManagerId);
        Assert.Equal("Test Manager", response.ManagerName);
        Assert.Equal(manager.Id, db.Departments.Find(it.Id)!.ManagerId);
    }

    [Fact]
    public async Task AssignManager_DepartmentMissing_NotFound()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        db.Users.Add(manager);
        db.SaveChanges();
        var controller = new DepartmentsController(db);
        
        var result = await controller.AssignManager(999, new AssignManagerRequest { UserId = manager.Id });
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFound);
    }

    [Fact]
    public async Task AssignManager_UserNotFound_Refused()
    {
        using var db = TestData.CreateDb();
        var it = new Department { Name = "IT" };
        db.Departments.Add(it);
        db.SaveChanges();
        var controller = new DepartmentsController(db);
        
        var result = await controller.AssignManager(it.Id, new AssignManagerRequest { UserId = 999 });
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User not found.", badRequest.Value);
    }

    [Fact]
    public async Task AssignManager_InactiveUser_Refused()
    {
        using var db = TestData.CreateDb();
        var it = new Department { Name = "IT" };
        db.Departments.Add(it);
        db.SaveChanges();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3, DepartmentId = it.Id, IsActive = false};
        db.Users.Add(manager);
        db.SaveChanges();
        var controller = new DepartmentsController(db);
        
        var result = await controller.AssignManager(it.Id, new AssignManagerRequest { UserId = manager.Id });
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The user is not active.", badRequest.Value);
    }
    
    [Fact]
    public async Task AssignManager_NotAManager_Refused()
    {
        using var db = TestData.CreateDb();
        var it = new Department { Name = "IT" };
        db.Departments.Add(it);
        db.SaveChanges();
        var user = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 2, DepartmentId = it.Id};
        db.Users.Add(user);
        db.SaveChanges();
        var controller = new DepartmentsController(db);
        
        var result = await controller.AssignManager(it.Id, new AssignManagerRequest { UserId = user.Id });
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The user is not a department manager.", badRequest.Value);
    }
    
    [Fact]
    public async Task AssignManager_OtherDepartment_Refused()
    {
        using var db = TestData.CreateDb();
        var it = new Department { Name = "IT" };
        var hr = new Department { Name = "HR" };
        db.Departments.Add(it);
        db.Departments.Add(hr);
        db.SaveChanges();
        var user = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3, DepartmentId = it.Id};
        db.Users.Add(user);
        db.SaveChanges();
        var controller = new DepartmentsController(db);
        
        var result = await controller.AssignManager(hr.Id, new AssignManagerRequest { UserId = user.Id });
        
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The user does not belong to this department.", badRequest.Value);
    }
}