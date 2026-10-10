using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/auth/login 
    // first it checks the email and password and then sets the login cookie
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // finding the user whose email equals the email sent in the login request
        // we are retrieving the role because we're going to put it in the authentication cookie 
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .SingleOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !user.IsActive)
            return Unauthorized("Invalid email or password.");

        var result = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        // facts about the user that are stored inside the cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // successful login, return 200 OK
        return Ok(UserResponse.FromUser(user));
    }

    // POST /api/auth/logout
    // remove the authentication cookie
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    // GET /api/auth/me 
    // returns the logged-in user
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // retrieve the NameIdentifier claim for the currently authenticated user
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .SingleOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive)
            return Unauthorized();

        return Ok(UserResponse.FromUser(user));
    }
}
