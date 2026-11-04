using System.Security.Claims;
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

    [HttpGet("manager")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> GetManagerDashboard()
    {
        User user = await LoadCurrentUser();

        int active = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int totalItems = active + expiringSoon + expired + cancelled;

        // requests to review
        List<RenewalRequest> reviewRequests = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.PendingManagerApproval && r.Period.Item.DepartmentId == user.DepartmentId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsToReview = new List<DashboardRequest>();
        foreach (RenewalRequest reviewRequest in reviewRequests)
        {
            requestsToReview.Add(ToDashboardRequest(reviewRequest));
        }

        // requests to approve
        List<RenewalRequest> awaitingCompletion = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.Approved && r.Period.Item.DepartmentId == user.DepartmentId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsAwaitingCompletion = new List<DashboardRequest>();
        foreach (RenewalRequest request in awaitingCompletion)
        {
            requestsAwaitingCompletion.Add(ToDashboardRequest(request));
        }

        // upcoming expirations
        List<Item> expiringItems = await _db.Items
            .Where(i => i.LifecycleState == LifecycleState.ExpiringSoon && i.DepartmentId == user.DepartmentId)
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
        return Ok(new ManagerDashboardResponse
        {
            TotalItems = totalItems,
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            RequestsToReview = requestsToReview,
            RequestsAwaitingCompletion = requestsToApprove,
            UpcomingExpirations = upcomingExpirations
        });
    }

    private static DashboardRequest ToDashboardRequest(RenewalRequest renewalRequest)
    {
        return new DashboardRequest
        {
            Id = renewalRequest.Id,
            ItemName = renewalRequest.Period.Item.Name,
            Status = renewalRequest.Status,
            NeedsFinancialReview = renewalRequest.NeedsFinancialReview,
            ProposedExpirationDate = renewalRequest.ProposedExpirationDate,
            SubmittedAt = renewalRequest.SubmittedAt,
            ProposedCost = renewalRequest.ProposedCost,
            RequestedByUserName =
                renewalRequest.RequestedByUser.FirstName + " " + renewalRequest.RequestedByUser.LastName,
        };
    }
}