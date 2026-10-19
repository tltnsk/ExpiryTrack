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

    // GET /api/requests/{id}
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
    // GET /api/items/{itemId}/requests
    [HttpGet("items/{itemId:int}/requests")]
    public async Task<IActionResult> GetForItem(int itemId)
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
    
    // all requests 
    [HttpGet("requests")]
    public async Task<IActionResult> GetAllRequests()
    {
        User currentUser = await LoadCurrentUser();
        List<RenewalRequest> renewalRequests = new List<RenewalRequest>();

        if (currentUser.Role.Name == "Employee")
        {
            renewalRequests = await _db.RenewalRequests
                .Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .Where(r => r.Period.Item.ResponsibleUserId == currentUser.Id)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        } else if (currentUser.Role.Name == "DepartmentManager")
        {
            renewalRequests = await _db.RenewalRequests
                .Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .Where(r => r.Period.Item.DepartmentId == currentUser.DepartmentId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        } else if (currentUser.Role.Name == "FinanceOfficer")
        {
            renewalRequests = await _db.RenewalRequests
                .Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .Where(r => r.NeedsFinancialReview)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        } else if (currentUser.Role.Name == "Administrator")
        {
            renewalRequests = await _db.RenewalRequests
                .Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        }
        var responses = new List<RenewalRequestResponse>();
        foreach (var renewalRequest in renewalRequests)
        {
            responses.Add(RenewalRequestResponse.FromRequest(renewalRequest));
        }
        return Ok(responses);
    }
    
    [HttpPost("requests/{id:int}/finance-review")]
    [Authorize(Roles = "FinanceOfficer")]
    public async Task<IActionResult> PostFinanceReview(int id, FinanceReviewRequest data)
    {
        RenewalResult result = await _rs.FinanceReviewAsync(id, GetCurrentUserId(), data);

        if (result.NotFound) return NotFound();
        
        if (result.Error != null) return BadRequest(result.Error);

        return Ok(RenewalRequestResponse.FromRequest(result.Request!));
    }

    [HttpPost("requests/{id:int}/approve")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> PostApprove(int id, ManagerDecisionRequest data)
    {
        RenewalResult result = await _rs.ApproveAsync(id, GetCurrentUserId(), data);
        
        if (result.NotFound) return NotFound();
        
        if (result.Error != null) return BadRequest(result.Error);
        
        return Ok(RenewalRequestResponse.FromRequest(result.Request!));
    }
    
    [HttpPost("requests/{id:int}/reject")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> PostReject(int id, ManagerDecisionRequest data)
    {
        RenewalResult result = await _rs.RejectAsync(id, GetCurrentUserId(), data);
        
        if (result.NotFound) return NotFound();
        
        if (result.Error != null) return BadRequest(result.Error);
        
        return Ok(RenewalRequestResponse.FromRequest(result.Request!));
    }
    
    [HttpPost("requests/{id:int}/request-clarification")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> PostClarification(int id, ManagerDecisionRequest data)
    {
        RenewalResult result = await _rs.ClarificationAsync(id, GetCurrentUserId(), data);
        
        if (result.NotFound) return NotFound();
        
        if (result.Error != null) return BadRequest(result.Error);
        
        return Ok(RenewalRequestResponse.FromRequest(result.Request!));
    }

    [HttpPost("requests/{id:int}/provide-clarification")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> PostProvideClarification(int id, ClarificationAnswerRequest data)
    {
        RenewalResult result = await _rs.AnswerClarificationAsync(id, GetCurrentUserId(), data);
        if (result.NotFound) return NotFound();
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(RenewalRequestResponse.FromRequest((result.Request!)));
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

        // finance officers can only see the requests for items that need financial review
        if (currentUser.Role.Name == "FinanceOfficer")
            return request.NeedsFinancialReview;

        // admins can see all the requests 
        return true;
    }

}