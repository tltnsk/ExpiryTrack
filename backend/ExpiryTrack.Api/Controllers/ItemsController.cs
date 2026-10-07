using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;


namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    // helper method for getting the user id from the cookie claims
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    // load the logged-in user along with their role
    private async Task<User> LoadCurrentUser()
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == GetCurrentUserId());
    }

    // helper method that returns whether the user can see the item 
    private static bool CanSee(Item item, User currentUser)
    {
        // employees can only see the items they're responsible for
        if (currentUser.Role.Name == "Employee")
            return item.ResponsibleUserId == currentUser.Id;
        
        // department managers can see the items in their department
        if (currentUser.Role.Name == "DepartmentManager")
            return item.DepartmentId == currentUser.DepartmentId;

        // admins and finance officers can see all the items 
        return true;
    }

public ItemsController(AppDbContext db)
    {
        _db = db;
    }

    // get an item by its id 
    // GET /api/items/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Item? item = await LoadItem(id);
        User currentUser = await LoadCurrentUser();
        
        // if the item does not exist or the user is not allowed to see the item return NotFound 
        if (item == null || !CanSee(item, currentUser))
        {
            return NotFound();
        }
        return Ok(ToResponse(item));
    }
    
    // get all the items (role based), the ones expiring soonest first
    // GET /api/items
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        User currentUser = await LoadCurrentUser();
        List<Item> items;

        if (currentUser.Role.Name == "Employee")
        {
            items = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Department)
                .Include(i => i.ResponsibleUser)
                .Include(i => i.CurrentPeriod)
                .Where(i => i.ResponsibleUserId == currentUser.Id)
                .OrderBy(i => i.CurrentPeriod.ExpirationDate)
                .ToListAsync();
        } else if (currentUser.Role.Name == "DepartmentManager")
        {
            items = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Department)
                .Include(i => i.ResponsibleUser)
                .Include(i => i.CurrentPeriod)
                .Where(i => i.DepartmentId == currentUser.DepartmentId)
                .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
                .ToListAsync();
        }
        else
        {
            items = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Department)
                .Include(i => i.ResponsibleUser)
                .Include(i => i.CurrentPeriod)
                .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
                .ToListAsync();
        }
        
        var responses = new List<ItemResponse>();
        foreach (var item in items)
        {
            responses.Add(ToResponse(item));
        }
        
        return Ok(responses);
    }
    
    // POST /api/items 
    // creating an item with its first period FR-ITEM-01, FR-ITEM-02, FR-ITEM-03, FR-ITEM-05
    // only employees and department managers can create an item 

    [Authorize(Roles = "Employee,DepartmentManager")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateItemRequest request)
    {
        // getting the current user
        var currentUser = await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == GetCurrentUserId());

        int responsibleUserId;
        int departmentId = currentUser.DepartmentId.Value;

        // if the current user is an employee, they're automatically responsible for the item they're creating 
        // the item belongs to their department too
        if (currentUser.Role.Name == "Employee")
        {
            responsibleUserId = currentUser.Id;
        } else 
        {
            // return bad request if responsible employee isn't specified
            if (request.ResponsibleUserId == null)
            {
                return BadRequest("Select the responsible employee.");
            }
            
            // load the responsible user from the database
            var responsibleUser = await _db.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.Id == request.ResponsibleUserId);

            if (responsibleUser == null)
            {
                return BadRequest("Responsible user not found.");
            }

            if (!responsibleUser.IsActive)
            {
                return BadRequest("Responsible user is not active.");
            }

            if (responsibleUser.Role.Name != "Employee")
            {
                return BadRequest("Responsible user must be an employee.");
            }

            if (responsibleUser.DepartmentId != currentUser.DepartmentId)
            {
                return BadRequest("The employee does not belong to your department.");
            }
            responsibleUserId = responsibleUser.Id;
        }
        
        var category = await _db.Categories.FindAsync(request.CategoryId);
        if (category == null || !category.IsActive)
        {
            return BadRequest("The category does not exist or it is not active.");
        }
        
        // get today's date 
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.ExpirationDate <= today)
        {
            return BadRequest("The expiration date must be a future date.");
        }

        if (request.StartDate >= request.ExpirationDate)
        {
            return BadRequest("The start date must be before the expiration date.");
        }
        
        // DayNumber returns how many days have passed since that date
        int daysLeft = request.ExpirationDate.DayNumber - today.DayNumber;
        
        // set the state based on how many days the item has left to the warning period

        var state = daysLeft <= category.WarningPeriodDays ? LifecycleState.ExpiringSoon : LifecycleState.Active;
        
        // for data consistency, we need to save everything in one transaction
        using var transaction = _db.Database.BeginTransaction();
        
        var now = DateTime.UtcNow;

        var item = new Item
        {
            ResponsibleUserId = responsibleUserId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Provider = request.Provider,
            ReferenceNumber = request.ReferenceNumber,
            CategoryId = category.Id,
            DepartmentId = departmentId,
            LifecycleState = state,
            StateChangedAt = now,
            CreatedAt = now,
        };
        
        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var period = new LifecyclePeriod
        {
            ItemId = item.Id,
            PeriodNumber = 1,
            StartDate = request.StartDate,
            ExpirationDate = request.ExpirationDate,
            Cost = request.Cost,
            CreatedAt = now,
        };
        
        _db.LifecyclePeriods.Add(period);
        await _db.SaveChangesAsync();
        
        item.CurrentPeriodId = period.Id;
        
        _db.ItemStateHistory.Add(new ItemStateHistory
        {
            ItemId = item.Id,
            FromState = null,
            ToState = state,
            ChangedByUserId = currentUser.Id,
            Reason = "Item created",
            ChangedAt = now,
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        
        var created = await LoadItem(item.Id);
        return Created($"/api/items/{item.Id}", ToResponse(created!));
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
    
    // helper method for loading an item
    private async Task<Item?> LoadItem(int id)
    {
        return await _db.Items
            .Include(i => i.Category)
            .Include(i => i.Department)
            .Include(i => i.ResponsibleUser)
            .Include(i => i.CurrentPeriod)
            .SingleOrDefaultAsync(i => i.Id == id);
    }
}