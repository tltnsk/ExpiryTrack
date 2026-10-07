using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;


namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ItemsController(AppDbContext db)
    {
        _db = db;
    }

    // get an item by its id 
    // GET /api/items/{id]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Item? item = await _db.Items
                        .Include(i => i.Category)
                        .Include(i => i.Department)
                        .Include(i => i.ResponsibleUser)
                        .Include(i => i.CurrentPeriod)
                        .SingleOrDefaultAsync(i => i.Id == id);
        if (item != null)
        {
            return Ok(ToResponse(item));
        }
        // return error if item doesn't exist 
        return NotFound();
    }
    
    // DTO returned by API 
    private static ItemResponse ToResponse(Item item)
    {
        return new ItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Provider = item.Provider,
            ReferenceNumber = item.ReferenceNumber,
            LifecycleState = item.LifecycleState,
            StateChangedAt = item.StateChangedAt,
            CreatedAt = item.CreatedAt,
            CategoryId = item.CategoryId,
            CategoryName = item.Category.Name,
            DepartmentId = item.DepartmentId,
            DepartmentName = item.Department.Name,
            ResponsibleUserId = item.ResponsibleUserId,
            ResponsibleUserName = item.ResponsibleUser.FirstName + " " + item.ResponsibleUser.LastName,
            PeriodNumber = item.CurrentPeriod!.PeriodNumber,
            StartDate = item.CurrentPeriod.StartDate,
            ExpirationDate = item.CurrentPeriod.ExpirationDate,
            Cost = item.CurrentPeriod.Cost
        };
    }
}