using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Services;
public class ItemService
{
    private readonly AppDbContext _db;

    public ItemService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<string?> CheckNewItem(User currentUser, CreateItemRequest request)
    {
        if (currentUser.DepartmentId == null)
        {
            return "You do not belong to a department.";
        }
        // Determine the responsible employee and department
        // if the current user is an employee, they're automatically responsible for the item they're creating 
        if (currentUser.Role.Name != "Employee")
        {
            if (request.ResponsibleUserId == null)
            {
                return "Select the responsible employee.";
            }
            
            var responsibleUser = await _db.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.Id == request.ResponsibleUserId);
            
            if (responsibleUser == null)
            {
                return "Select the responsible employee.";
            }
            
            if (!responsibleUser.IsActive)
            {
                return "Responsible user is not active.";
            }
            if (responsibleUser.Role.Name != "Employee")
            {
                return "Responsible user must be an employee.";
            }

            if (responsibleUser.DepartmentId != currentUser.DepartmentId)
            {
                return "Responsible user does not belong to your department.";
            }
        }
        // the category must exist and be active
        var category = await _db.Categories.FindAsync(request.CategoryId);

        // check that the category actually exists and is active
        if (category == null || !category.IsActive)
        {
            return "The category does not exist or it is not active.";
        }

        // check the dates
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.ExpirationDate <= today)
        {
            return "The expiration date must be a future date.";
        }

        if (request.StartDate >= request.ExpirationDate)
        {
            return "The start date must be before the expiration date.";
        }
        
        // if there aren't any issues
        return null;
    }
    
    private async Task<Item> AddNewItem(User currentUser, CreateItemRequest request)
    {
        int responsibleUserId = currentUser.Role.Name == "Employee" ? currentUser.Id : request.ResponsibleUserId!.Value;
        int departmentId = currentUser.DepartmentId!.Value;
        var category = await _db.Categories.FindAsync(request.CategoryId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
 
        // calculate how many days are left until expiration
        int daysLeft = request.ExpirationDate.DayNumber - today.DayNumber;

        // set the state based on how many days the item has left to the warning period
        var state = daysLeft <= category.WarningPeriodDays ? LifecycleState.ExpiringSoon : LifecycleState.Active;
        
        var now = DateTime.UtcNow;

        // create the item 
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

        // create the first lifecycle period
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

        // set the current period 
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
        return item;
    }

    public async Task<ItemResult> CreateAsync(int userId, CreateItemRequest request)
    { 
        // load the logged in user
        var currentUser = await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == userId);
        
        // check the new item
        string? error = await CheckNewItem(currentUser, request);
        if (error != null)
        {
            return new ItemResult {Error = error};
        }
        
        // save item, first period and history in one transaction
        await using var transaction = await _db.Database.BeginTransactionAsync();
        Item item = await AddNewItem(currentUser, request);
        await transaction.CommitAsync();
        
        return new ItemResult {Item = item};
    }
    
    public async Task<ItemResult> ImportAsync(int userId, Stream file)
    {
        throw new NotImplementedException();
    }
    
}