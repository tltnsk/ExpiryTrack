using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;


namespace ExpiryTrack.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ItemService _itemService;

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

    public ItemsController(AppDbContext db, ItemService itemService)
    {
        _db = db;
        _itemService = itemService;
    }

    // get an item by its id 
    // GET /api/items/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        Item? item = await LoadItem(id);
        User currentUser = await LoadCurrentUser();

        // if the item does not exist or the user is not allowed to access the item return NotFound 
        if (item == null || !AccessRules.CanSeeItem(item, currentUser))
        {
            return NotFound();
        }
        return Ok(ItemResponse.ToResponse(item));
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
                .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
                .ToListAsync();
        }
        else if (currentUser.Role.Name == "DepartmentManager")
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
        else if (currentUser.Role.Name == "FinanceOfficer")
        {
            items = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Department)
                .Include(i => i.ResponsibleUser)
                .Include(i => i.CurrentPeriod)
                // finance officers can only see items if the current period has a cost
                .Where(i => i.CurrentPeriod!.Cost != null)
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
            responses.Add(ItemResponse.ToResponse(item));
        }

        return Ok(responses);
    }

    // POST /api/items 
    // creating an item with its first period (FR-ITEM-01, FR-ITEM-02, FR-ITEM-03, FR-ITEM-05)
    // only employees and department managers can create an item 

    [Authorize(Roles = "Employee,DepartmentManager")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateItemRequest request)
    {
        int userId = GetCurrentUserId();
        ItemResult itemResult = await _itemService.CreateAsync(userId, request);
        if (itemResult.Error != null) return BadRequest(itemResult.Error);
        Item? created = await LoadItem(itemResult.Item!.Id);
        return Created($"/api/items/{created!.Id}", ItemResponse.ToResponse(created));
    }
    
    // POST /api/items/import
    [HttpPost("import")]
    [Authorize(Roles = "DepartmentManager")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new {errors = new[] {"Choose a file to import."}});
        
        if (file.Length > 2 * 1024 * 1024) return BadRequest(new {errors = new[] { "The file must be 2MB or smaller." }});

        if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
        {
            return BadRequest(new { errors = new[] {"You can only upload an XLSX file."}});
        }
        using var stream = file.OpenReadStream();
        ItemResult result = await _itemService.ImportAsync(GetCurrentUserId(), stream);
        
        if (result.Error != null) return BadRequest(new {errors = new[] {result.Error}});
        if (result.RowErrors.Count > 0) return BadRequest(new {errors = result.RowErrors});
        return Ok(new {createdCount = result.CreatedCount});
    }

    // updating an item
    // only employee and department manager can update the items that are accessible for them 
    // they can only update the descriptive fields
    // PUT /api/items/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Employee,DepartmentManager")]
    public async Task<IActionResult> Update(int id, UpdateItemRequest request)
    {
        ItemResult result = await _itemService.UpdateAsync(id, GetCurrentUserId(), request);
        if (result.NotFound) return NotFound();
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(ItemResponse.ToResponse(result.Item!));
    }

    // reassigning an item
    // only a department manager can reassign an item to another employee
    // PUT /api/items/{id}/reassign
    [Authorize(Roles = "DepartmentManager")]
    [HttpPut("{id}/reassign")]
    public async Task<IActionResult> Reassign(int id, ReassignItemRequest request)
    {
        ItemResult result = await _itemService.ReassignAsync(id, GetCurrentUserId(), request);
        if (result.NotFound) return NotFound();
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(ItemResponse.ToResponse(result.Item!));
    }

    // cancelling an item
    // only the manager can cancel an item
    // the item needs to be expiring soon or active with no open renewal requests
    // PUT /api/items/{id}/cancel
    [Authorize(Roles = "DepartmentManager")]
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        ItemResult result = await _itemService.CancelAsync(id, GetCurrentUserId());
        if (result.NotFound) return NotFound();
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(ItemResponse.ToResponse(result.Item!));
    }

    // getting period info for an item 
    [HttpGet("{id:int}/periods")]
    public async Task<IActionResult> GetPeriods(int id)
    {
        User currentUser = await LoadCurrentUser();
        Item? item = await LoadItem(id);

        if (item == null || !AccessRules.CanSeeItem(item, currentUser))
        {
            return NotFound();
        }

        List<LifecyclePeriod> periods = await _db.LifecyclePeriods
            .Where(p => p.ItemId == id)
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        var responses = new List<PeriodResponse>();

        foreach (var period in periods)
        {
            var response = PeriodResponse.ToPeriodResponse(period);
            responses.Add(response);
        }

        return Ok(responses);
    }

    // getting history info for one item
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        User currentUser = await LoadCurrentUser();
        Item? item = await LoadItem(id);
        if (item == null || !AccessRules.CanSeeItem(item, currentUser))
        {
            return NotFound();
        }

        List<ItemStateHistory> histories = await _db.ItemStateHistory
            .Where(h => h.ItemId == id)
            .Include(u => u.ChangedByUser)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

        var responses = new List<StateHistoryResponse>();
        foreach (var history in histories)
        {
            var response = StateHistoryResponse.ToStateHistoryResponse(history);
            responses.Add(response);
        }

        return Ok(responses);
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