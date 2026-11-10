using System.Security.Claims;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserService _us;

    public UsersController(AppDbContext db, UserService us)
    {
        _db = db;
        _us = us;
    }

    // helper method for getting the user id from the authentication claims
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // load the logged-in user along with their role
    private async Task<User> LoadCurrentUser()
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == GetCurrentUserId());
    }

    // GET /api/users
    // get all users with their role and department 
    // FR-ADM-01
    [HttpGet]
    [Authorize(Roles = "Administrator")]
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
    [Authorize(Roles = "Administrator")]
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
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        UserResult result = await _us.CreateAsync(request);
        if (result.Conflict != null) return Conflict(result.Conflict);
        if (result.Error != null) return BadRequest(result.Error);
        return Created($"/api/users/{result.User!.Id}", UserResponse.FromUser(result.User));
    }

    // PUT /api/users/{id} 
    // update name and email 
    // FR-ADM-01, FR-ADM-03
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        UserResult result = await _us.UpdateAsync(id, request);
        if (result.NotFound) return NotFound();
        if (result.Conflict != null) return Conflict(result.Conflict);
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(UserResponse.FromUser(result.User!));
    }

    // PUT /api/users/{id}/active 
    // activate or deactivate a user (FR-ADM-01)
    [HttpPut("{id}/active")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SetActive(int id, SetUserActiveRequest request)
    {
        UserResult result = await _us.SetActiveAsync(id, GetCurrentUserId(), request);
        if (result.NotFound) return NotFound();
        if (result.Conflict != null) return Conflict(result.Conflict);
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(UserResponse.FromUser(result.User!));
    }

    // get the employees from the current manager's department
    [HttpGet("employees")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> GetDepartmentEmployees()
    {
        User currentUser = await LoadCurrentUser();
        var users = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Where(u => u.Role.Name == "Employee")
            .Where(u => u.DepartmentId == currentUser.DepartmentId)
            .Where(u => u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var responses = new List<UserResponse>();
        foreach (var user in users)
        {
            responses.Add(UserResponse.FromUser(user));
        }
        return Ok(responses);
    }
}