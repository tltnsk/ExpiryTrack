using System.Security.Claims;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _ds;

    public DashboardController(DashboardService ds)
    {
        _ds = ds;
    }

    // helper method for getting the user id from the authentication claims
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        return Ok(await _ds.GetAdminDashboardAsync());
    }

    [HttpGet("manager")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> GetManagerDashboard()
    {
        return Ok(await _ds.GetManagerDashboardAsync(GetCurrentUserId()));
    }

    [HttpGet("employee")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetEmployeeDashboard()
    {
        return Ok(await _ds.GetEmployeeDashboardAsync(GetCurrentUserId()));
    }

    [HttpGet("finance")]
    [Authorize(Roles = "FinanceOfficer")]
    public async Task<IActionResult> GetFinanceDashboard()
    {
        return Ok(await _ds.GetFinanceDashboardAsync());
    }
}