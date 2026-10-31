using System.Security.Claims;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Controllers;


[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : Controller
{
    private readonly AppDbContext _db;
    
    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }
    
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
    
    // GET api/notifications
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var notifications = await _db.Notifications
            .Where(n => n.RecipientUserId == GetCurrentUserId())
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        
        var responses = new List<NotificationResponse>();
        foreach (var notification in notifications)
        {
            var response = ToResponse(notification);
            responses.Add(response);
        }
        
        return Ok(responses);
    }
    
    //  GET api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnread()
    {
        int count = await _db.Notifications
            .Where(n => n.RecipientUserId == GetCurrentUserId() && !n.IsRead)
            .CountAsync();
        
        return Ok(count);
    }
    
    // PUT api/notifications/{id:int}/read
    // set that a notification is read
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> UpdateRead(int id)
    {
        var notification  = await _db.Notifications
            .FindAsync(id);

        if (notification == null || notification.RecipientUserId != GetCurrentUserId())
        {
            return NotFound();
        }
        
        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(notification));
    }

    private static NotificationResponse ToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ItemId = notification.ItemId,
            RequestId = notification.RequestId
        };
    }
}