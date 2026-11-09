using ClosedXML.Excel;
using System.Security.Claims;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Services;

public class ItemService
{
    private readonly AppDbContext _db;

    public ItemService(AppDbContext db)
    {
        _db = db;
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
    
    // load the logged-in user along with their role
    private async Task<User> LoadCurrentUser(int userId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == userId);
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
                return "Responsible user not found.";
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
        var category = (await _db.Categories.FindAsync(request.CategoryId))!;
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
            return new ItemResult { Error = error };
        }

        // save item, first period and history in one transaction
        await using var transaction = await _db.Database.BeginTransactionAsync();
        Item item = await AddNewItem(currentUser, request);
        await transaction.CommitAsync();

        return new ItemResult { Item = item };
    }

    public async Task<ItemResult> UpdateAsync(int id, int userId, UpdateItemRequest request)
    {
        // load the item and the current user
        Item? item = await LoadItem(id);
        User currentUser = await LoadCurrentUser(userId);

        // if the item doesn't belong to the user's department 
        if (item == null || !AccessRules.CanSeeItem(item, currentUser))
        {
            return new ItemResult { NotFound = true};
        }

        // users can only update expiring soon or active items 
        if (item.LifecycleState != LifecycleState.ExpiringSoon && item.LifecycleState != LifecycleState.Active)
        {
            return new ItemResult { Error = "Only Active and Expiring Soon items can be updated." };
        }

        item.Name = request.Name.Trim();
        item.Description = request.Description;
        item.Provider = request.Provider;
        item.ReferenceNumber = request.ReferenceNumber;

        await _db.SaveChangesAsync();

        return new ItemResult { Item = item };
    }
    
    public async Task<ItemResult> ReassignAsync(int id, int userId, ReassignItemRequest request)
    {
        // the manager who reassigns the item 
        User assignerUser = await LoadCurrentUser(userId);

        // employee the item is being reassigned to 
        User? assignedUser = await _db.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == request.ResponsibleUserId);

        Item? item = await LoadItem(id);

        // the manager can only reassign items from their department
        if (item == null || !AccessRules.CanSeeItem(item, assignerUser))
        {
            return new ItemResult { NotFound = true};
        }

        // only active and expiring soon items can be reassigned
        if (item.LifecycleState != LifecycleState.ExpiringSoon && item.LifecycleState != LifecycleState.Active)
        {
            return new ItemResult { Error = "Only Active and Expiring Soon items can be reassigned." };
        }

        if (assignedUser == null)
        {
            return new ItemResult { Error = "User not found." };
        }

        if (!assignedUser.IsActive)
        {
            return new ItemResult { Error = "User is not active." };
        }

        if (assignedUser.Role.Name != "Employee")
        {
            return new ItemResult { Error = "The user is not an employee." };
        }

        if (assignedUser.DepartmentId != assignerUser.DepartmentId)
        {
            return new ItemResult { Error = "The user does not belong to your department." };
        }

        item.ResponsibleUserId = assignedUser.Id;
        item.ResponsibleUser = assignedUser;

        await _db.SaveChangesAsync();
        return new ItemResult { Item = item };
    }

    public async Task<ItemResult> CancelAsync(int id, int userId)
    {
        User currentUser = await LoadCurrentUser(userId);
        Item? item = await LoadItem(id);

        if (item == null || !AccessRules.CanSeeItem(item, currentUser))
        {
            return new ItemResult { NotFound = true};
        }

        if (item.LifecycleState != LifecycleState.ExpiringSoon && item.LifecycleState != LifecycleState.Active)
        {
            return new ItemResult { Error = "Only Active and Expiring Soon items can be cancelled." };
        }

        bool hasOpenRequest = await _db.RenewalRequests.AnyAsync(r =>
            r.PeriodId == item.CurrentPeriodId && r.Status != RequestStatus.Rejected && r.Status != RequestStatus.Completed);
        if (hasOpenRequest)
        {
            return new ItemResult { Error = "You cannot cancel the item because it has a renewal request open." };
        }

        var now = DateTime.UtcNow;

        // keeping the previous state for the history record
        LifecycleState oldState = item.LifecycleState;
        item.LifecycleState = LifecycleState.Cancelled;
        item.StateChangedAt = now;

        _db.ItemStateHistory.Add(new ItemStateHistory
        {
            ItemId = item.Id,
            FromState = oldState,
            ToState = LifecycleState.Cancelled,
            ChangedByUserId = currentUser.Id,
            Reason = "Item cancelled",
            ChangedAt = now,
        });

        await _db.SaveChangesAsync();
        return new ItemResult { Item = item };
    }

    public async Task<ItemResult> ImportAsync(int userId, Stream file)
    {
        var result = new ItemResult();
        var currentUser = await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == userId);

        // open the Excel file
        XLWorkbook workbook;
        try
        {
            // open the .xlsx
            workbook = new XLWorkbook(file);
        }
        catch
        {
            return new ItemResult { Error = "The file is not a valid Excel file." };
        }

        using (workbook)
        {
            var sheet = workbook.Worksheet(1); // the first sheet

            string[] headers =
            {
                "Name", "Category", "Responsible email", "Start date", "Expiration date", "Cost", "Provider",
                "Reference number", "Description"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                if (sheet.Cell(1, i + 1).GetString().Trim() != headers[i])
                {
                    return new ItemResult { Error = "The file doesn't match the template." };
                }
            }

            var lastUsedRow = sheet.LastRowUsed();
            if (lastUsedRow == null || lastUsedRow.RowNumber() < 2)
            {
                return new ItemResult { Error = "The file is empty." };
            }
            int lastRow = lastUsedRow.RowNumber();

            // the rows that could be read
            var requests = new List<(int Row, CreateItemRequest Request)>();

            for (int row = 2; row <= lastRow; row++)
            {
                if (sheet.Row(row).IsEmpty()) continue;

                var name = sheet.Cell(row, 1).GetString().Trim();
                if (name == "")
                {
                    result.RowErrors.Add($"Row {row}: Name is required.");
                    continue;
                }

                if (name.Length > 150)
                {
                    result.RowErrors.Add($"Row {row}: Name can be up to 150 characters long.");
                    continue;
                }

                var categoryName = sheet.Cell(row, 2).GetString().Trim();
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                if (category == null)
                {
                    result.RowErrors.Add($"Row {row}: Category doesn't exist.");
                    continue;
                }

                var responsibleEmail = sheet.Cell(row, 3).GetString().Trim();
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == responsibleEmail);
                if (user == null)
                {
                    result.RowErrors.Add($"Row {row}: No user with this email.");
                    continue;
                }

                if (!sheet.Cell(row, 4).TryGetValue(out DateTime startDate))
                {
                    result.RowErrors.Add($"Row {row}: Start date is invalid.");
                    continue;
                }

                if (!sheet.Cell(row, 5).TryGetValue(out DateTime expirationDate))
                {
                    result.RowErrors.Add($"Row {row}: Expiration date is invalid.");
                    continue;
                }

                decimal? cost = null;
                if (!sheet.Cell(row, 6).IsEmpty())
                {
                    if (!sheet.Cell(row, 6).TryGetValue(out decimal costValue) || costValue <= 0)
                    {
                        result.RowErrors.Add($"Row {row}: Cost must be greater than zero.");
                        continue;
                    }
                    cost = costValue;
                }

                var provider = sheet.Cell(row, 7).GetString().Trim();
                if (provider.Length > 150)
                {
                    result.RowErrors.Add($"Row {row}: Provider can be up to 150 characters long.");
                    continue;
                }

                var referenceNumber = sheet.Cell(row, 8).GetString().Trim();
                if (referenceNumber.Length > 100)
                {
                    result.RowErrors.Add($"Row {row}: Reference Number can be up to 100 characters long.");
                    continue;
                }
                var description = sheet.Cell(row, 9).GetString().Trim();

                requests.Add((row, new CreateItemRequest
                {
                    Name = name,
                    CategoryId = category.Id,
                    ResponsibleUserId = user.Id,
                    StartDate = DateOnly.FromDateTime(startDate),
                    ExpirationDate = DateOnly.FromDateTime(expirationDate),
                    Cost = cost,
                    Provider = provider == "" ? null : provider,
                    ReferenceNumber = referenceNumber == "" ? null : referenceNumber,
                    Description = description == "" ? null : description,
                }));
            }

            foreach (var (row, request) in requests)
            {
                string? error = await CheckNewItem(currentUser, request);
                if (error != null)
                {
                    result.RowErrors.Add($"Row {row}: {error}");
                }
            }

            // if some rows couldn't be read nothing is impored
            if (result.RowErrors.Count > 0)
            {
                return result;
            }

            // if all rows are valid save the items in one transaction
            await using var transaction = await _db.Database.BeginTransactionAsync();
            foreach (var (row, request) in requests)
            {
                await AddNewItem(currentUser, request);
            }
            await transaction.CommitAsync();

            result.CreatedCount = requests.Count;
        }
        return result;
    }
}