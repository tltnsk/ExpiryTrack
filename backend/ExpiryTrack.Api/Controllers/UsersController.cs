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
    public async Task<IActionResult> Get()
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
}