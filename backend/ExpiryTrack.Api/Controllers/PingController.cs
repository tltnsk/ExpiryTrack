using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/ping")]
public class PingController : ControllerBase
{
    // a test endpoint to check that the API is running 
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok("pong");
    }
}