using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Roles = "Administrator")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/departments
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // we have to load the manager name together with the department
        // otherwise d.Manager is null 
        var departments = await _db.Departments
            .Include(d => d.Manager)
            .OrderBy(d => d.Name)
            .ToListAsync();

        var responses = new List<DepartmentResponse>();
        foreach (var department in departments)
        {
            var response = ToResponse(department);
            responses.Add(response);
        }
        return Ok(responses);
    }

    // GET /api/departments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var department = await _db.Departments
            .Include(d => d.Manager)
            .SingleOrDefaultAsync(d => d.Id == id);
        if (department == null)
            return NotFound();

        DepartmentResponse response = ToResponse(department);
        return Ok(response);
    }

    // POST /api/departments
    [HttpPost]
    public async Task<IActionResult> Create(DepartmentRequest departmentRequest)
    {
        var name = departmentRequest.Name.Trim();

        if (await _db.Departments.AnyAsync(d => d.Name == name))
            return Conflict("Name already exists.");

        var department = new Department();
        CopyFromRequest(departmentRequest, department);

        await _db.Departments.AddAsync(department);
        await _db.SaveChangesAsync();

        return Created($"/api/departments/{department.Id}", ToResponse(department));
    }

    // PUT /api/departments/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, DepartmentRequest departmentRequest)
    {
        var department = await _db.Departments
            .Include(d => d.Manager)
            .SingleOrDefaultAsync(d => d.Id == id);
        if (department == null)
            return NotFound();

        var name = departmentRequest.Name.Trim();
        if (await _db.Departments.AnyAsync(d => d.Name == name && d.Id != id))
            return Conflict("Department already exists.");

        CopyFromRequest(departmentRequest, department);
        await _db.SaveChangesAsync();
        return Ok(ToResponse(department));
    }

    // PUT /api/department/{id}/manager
    // assigning a manager to a deparment
    [HttpPut("{id}/manager")]
    public async Task<IActionResult> AssignManager(int id, AssignManagerRequest request)
    {
        var department = await _db.Departments.Include(d => d.Manager).SingleOrDefaultAsync(d => d.Id == id);

        if (department == null) return NotFound();

        // in the case when a department needs to temporarily not have a manager
        // for example, when deactivating a user 
        // an admin won't be able to deactivate a user if they're managing a department 
        if (request.UserId == null)
        {
            department.ManagerId = null;
            department.Manager = null;
            await _db.SaveChangesAsync();
            return Ok(ToResponse(department));
        }

        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null) return BadRequest("User not found.");

        if (!user.IsActive) return BadRequest("The user is not active.");

        if (user.Role.Name != "DepartmentManager") return BadRequest("The user is not a department manager.");

        if (user.DepartmentId != id) return BadRequest("The user does not belong to this department.");

        if (await _db.Departments.AnyAsync(d => d.ManagerId == request.UserId && d.Id != id))
            return Conflict("This user already manages another department.");

        department.ManagerId = user.Id;
        department.Manager = user;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(department));
    }

    private static DepartmentResponse ToResponse(Department department)
    {
        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            IsActive = department.isActive,
            ManagerId = department.ManagerId,
            ManagerName = department.Manager != null
                ? department.Manager.FirstName + " " + department.Manager.LastName : null
        };
    }

    private static void CopyFromRequest(DepartmentRequest request, Department department)
    {
        department.Name = request.Name;
        department.isActive = request.IsActive;
    }
}