using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    
    // load the logged-in user along with their role
    private async Task<User> LoadCurrentUser(int userId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == userId);
    }

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportResponse> GetReportAsync(int userId)
    {
        User currentUser = await LoadCurrentUser(userId);
        List<Item> items;

        // get the items for the user
        if (currentUser.Role.Name == "DepartmentManager")
        {
            items = await _db.Items
                .Include(i => i.Category)
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
                .Include(i => i.ResponsibleUser)
                .Include(i => i.CurrentPeriod)
                .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
                .ToListAsync();
        }
        
        // exclude cancelled items from all report calculations
        items = items.Where(i => i.LifecycleState != LifecycleState.Cancelled).ToList();

        // count items expiring within the next 30. 60 and 90 days
        ExpiringReport expiringReport = new();
        
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        foreach (Item item in items)
        {
            if (item.LifecycleState == LifecycleState.ExpiringSoon || item.LifecycleState == LifecycleState.Active)
            {
                int daysLeft = item.CurrentPeriod!.ExpirationDate.DayNumber - today.DayNumber;
                if (daysLeft < 0)
                {
                    continue;
                    
                }
                if (daysLeft <= 30)
                {
                    expiringReport.Within30Days++;
                }
                else if (daysLeft <= 60)
                {
                    expiringReport.Within60Days++;
                }
                else if (daysLeft <= 90)
                {
                    expiringReport.Within90Days++;
                }
            }
        }

        // group items by category and calculate the number of items and their total cost
        List<CategoryReportRow> categoryReportRows = new List<CategoryReportRow>();
        var groupedByCategory = items.GroupBy(i => i.Category);

        foreach (var group in groupedByCategory)
        {
            categoryReportRows.Add(new CategoryReportRow
            {
                CategoryName = group.Key.Name,
                ItemCount = group.Count(),
                TotalCost = group.Sum(i => i.CurrentPeriod!.Cost) ?? 0
            });
        }

        // find 5 providers with highest number of items
        List<TopReportRow> topProviders = new List<TopReportRow>();
        var groupedByProviders = items.Where(i => i.Provider != null).GroupBy(i => i.Provider);

        foreach (var group in groupedByProviders.OrderByDescending(g => g.Count()).Take(5))
        {
            topProviders.Add(new TopReportRow
            {
                Name = group.Key!,
                ItemCount = group.Count(),
            });
        }

        // 5 employees responsible for the most items
        List<TopReportRow> topEmployees = new List<TopReportRow>();
        var groupedByEmployees = items.GroupBy(i => i.ResponsibleUser);

        foreach (var group in groupedByEmployees.OrderByDescending(g => g.Count()).Take(5))
        {
            topEmployees.Add(new TopReportRow
            {
                Name = group.Key.FirstName + " " + group.Key.LastName,
                ItemCount = group.Count(),
            });
        }

        return new ReportResponse
        {
            Expiring = expiringReport,
            Categories = categoryReportRows,
            TopProviders = topProviders,
            TopEmployees = topEmployees
        };
    }
}