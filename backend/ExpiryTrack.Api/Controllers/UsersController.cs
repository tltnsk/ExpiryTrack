using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admnistrators")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/users
    // get all users with their role and department 
    // FR-ADM-01
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var responses = new List<UserResponse>();
        foreach (var user in users)
        {
            var response = UserResponse.FromUser(user);
            responses.Add(response);
        }
        return Ok(responses);
    }


    // GET /api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return Ok(UserResponse.FromUser(user));
    }

    // POST /api/users
    // create a user with initial password
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var email = request.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("A user with this email exists already.");

        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            return BadRequest("Unknown role.");

        var departmentError = await CheckDepartment(role.Name, request.DepartmentId);
        if (departmentError != null)
            return BadRequest(departmentError);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            RoleId = role.Id,
            DepartmentId = request.DepartmentId,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var created = await LoadUser(user.Id);
        return created($"/api/users/{user.Id}", UserResponse.FromUser(created!));
    }

    // PUT /api/users/{id} 
    // update name, email, role and department
    // FR-ADM-01, FR-ADM-03
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        var user = await LoadUser(id);

        if (user == null)
            return NotFound();

        var email = request.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id))
            return Conflict("A user with this email already exists.");

        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            return BadRequest("Unknown role.");

        var departmentError = await CheckDepartment(role.Name, request.DepartmentId);
        if (departmentError != null)
            return BadRequest(departmentError);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.RoleId = role.Id;
        user.DepartmentId = request.DepartmentId;
        await _db.SaveChangesAsync();

        var updated = await LoadUser(id);
        return Ok(UserResponse.FromUser(updated!));
    }

    // PUT /api/users/{id}/active 
    // activate or deactivate a user (FR-ADM-01)
    [HttpPut("{id}/active")]
    public async Task<IActionResult> SetActive(int id, SetUserActiveRequest request)
    {
        var user = await LoadUser(id);
        if (user == null)
            return NotFound();

        if (!request.IsActive)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == currentUserId)
                return Conflict("You cannot deactivate your own account.");

            // A user who still owns items or open requests can't be deactivated (U1)
            bool ownsItems = await _db.Items.AnyAsync(i => i.ResponsibleUserId == id
                && (i.LifecycleState == LifecycleState.Active || i.LifecycleState == LifecycleState.ExpiringSoon));
            if (ownsItems)
                return Conflict("This user is still responsible for items.");

            bool hasOpenRequests = await _db.RenewalRequests.AnyAsync(r => r.RequestedByUserId == id
                && r.Status != RequestStatus.Rejected && r.Status != RequestStatus.Completed);

            if (hasOpenRequests)
                return Conflict("This user has open renewal requests.");

            if (await _db.Departments.AnyAsync(d => d.ManagerId == id))
                return Conflict("This user manages a department. Assign another manager first.");
        }

        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return Ok(UserResponse.FromUser(user));
    }

    // helper method for loading user by id
    private async Task<User?> LoadUser(int id)
    {
        return await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .SingleOrDefaultAsync(u => u.Id == id);
    }

    // validator for departments
    // employees and managers need an active department
    // administrators and finance officers don't belong to a department
    // the method returns null if everything is fine
    private async Task<string?> CheckDepartment(string roleName, int? departmentId)
    {
        bool needsDepartment = roleName == "Employee" || roleName == "DepartmentManager";

        if (needsDepartment && departmentId == null)
            return "Employees and department managers must belong to a department.";

        if (!needsDepartment && departmentId != null)
            return "Administrators and finance officers do not belong to a department.";

        if (departmentId != null && !await _db.Departments.AnyAsync(d => d.Id == departmentId && d.IsActive))
            return "The department does not exist or is inactive.";

        return null;
    }
}