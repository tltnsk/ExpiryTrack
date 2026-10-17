using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class RenewalRequestsController : ControllerBase
{
    private readonly RenewalService _rs;

    public RenewalRequestsController(RenewalService rs)
    {
        _rs = rs;
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

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}