using System.Security.Claims;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportService _rs;

    public ReportsController(ReportService rs)
    {
        _rs = rs;
    }
    
    // helper method for getting the user id from the authentication claims
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
    
    [HttpGet]
    [Authorize(Roles = "Administrator,DepartmentManager,FinanceOfficer")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _rs.GetReportAsync(GetCurrentUserId()));
    }
}