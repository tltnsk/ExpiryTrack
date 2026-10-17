using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Services;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class RenewalRequestsController : ControllerBase
{
    private readonly RenewalService _rs;
    private readonly AppDbContext _db;

    public RenewalRequestsController(RenewalService rs, AppDbContext db)
    {
        _rs = rs;
        _db = db;
    }

    [HttpPost("items/{itemId}/requests")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Submit(int itemId, SubmitRenewalRequest data)
    {
        RenewalResult result = await _rs.SubmitAsync(itemId, GetCurrentUserId(), data);
        if (result.NotFound) return NotFound();
        if (result.Error != null) return BadRequest(result.Error);
        return Created($"/api/requests/{result.Request!.Id}", RenewalRequestResponse.FromRequest(result.Request!));
    }

    [HttpGet("requests/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        RenewalRequest? renewalRequest = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .FirstOrDefaultAsync(r => r.Id == id);

        User currentUser = await LoadCurrentUser();

        if (renewalRequest == null || !CanSeeRequest(renewalRequest, currentUser)) return NotFound();

        return Ok(RenewalRequestResponse.FromRequest(renewalRequest));
    }
    
    // all the requests for an item
    [HttpGet("items/{itemId:int}/requests")]
    public async Task<IActionResult> GetAllRequests(int itemId)
    {
        User currentUser =  await LoadCurrentUser();
        
        Item? item = await _db.Items
            .Include(i => i.CurrentPeriod)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        
        if (item == null) return NotFound();

        if (ItemsController.CanSee(item, currentUser))
        {
            List<RenewalRequest> renewalRequests;
        
            // Requests of all periods of the item
            renewalRequests = await _db.RenewalRequests
                .Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .Where(i => i.Period.ItemId == itemId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
            
            var responses = new List<RenewalRequestResponse>();
            foreach (var renewalRequest in renewalRequests)
            {
                if (CanSeeRequest(renewalRequest, currentUser))
                {
                    responses.Add(RenewalRequestResponse.FromRequest(renewalRequest));
                }
            }
            return Ok(responses);
        }
        return NotFound();
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private async Task<User> LoadCurrentUser()
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == GetCurrentUserId());
    }

    // helper method that returns whether the user has access to the item 
    private static bool CanSeeRequest(RenewalRequest request, User currentUser)
    {
        // employees can only see the requests for items they're responsible for
        if (currentUser.Role.Name == "Employee")
            return request.Period.Item.ResponsibleUserId == currentUser.Id;

        // department managers can see the requests for the items in their department
        if (currentUser.Role.Name == "DepartmentManager")
            return request.Period.Item.DepartmentId == currentUser.DepartmentId;

        // finance officers can only see the requeses for items that need financial review
        if (currentUser.Role.Name == "FinanceOfficer")
            return request.NeedsFinancialReview;

        // admins can see all the requests 
        return true;
    }

}