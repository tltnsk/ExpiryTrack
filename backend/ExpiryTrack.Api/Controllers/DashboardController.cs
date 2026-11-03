using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        int active = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int totalItems = active + expiringSoon + expired + cancelled;
        
        // items per department
        List<DepartmentItemCount> byDepartment = await _db.Departments
            .Select(d => new DepartmentItemCount
            {
                DepartmentName = d.Name,
                ItemCount = d.Items.Count
            })
            .ToListAsync();

        List<Item> expiringItems = await _db.Items
            .Where(i => i.LifecycleState == LifecycleState.ExpiringSoon)
            .Include(i => i.Department)
            .Include(i => i.CurrentPeriod)
            .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
            .Take(5)
            .ToListAsync();

        List<DashboardItem> upcomingExpirations = new List<DashboardItem>();
        foreach (Item item in expiringItems)
        {
            upcomingExpirations.Add(new DashboardItem
            {
                Id = item.Id,
                Name = item.Name,
                DepartmentName = item.Department.Name,
                ExpirationDate = item.CurrentPeriod!.ExpirationDate,
            });
        }
        return Ok(new AdminDashboardResponse
        {
            TotalItems = totalItems,
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            ItemsByDepartment = byDepartment,
            UpcomingExpirations = upcomingExpirations
        });
    }
}