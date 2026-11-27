using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Services;

public class DashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    // load the logged-in user along with their role
    private async Task<User> LoadCurrentUser(int userId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .SingleAsync(u => u.Id == userId);
    }

    // helper method for counting how many items expire in each of the 12 months
    public async Task<List<MonthCount>> CountExpirationsByMonth(IQueryable<Item> items, DateOnly today)
    {
        List<MonthCount> result = new List<MonthCount>();
        DateOnly firstMonth = new DateOnly(today.Year, today.Month, 1);

        // runs 12 times and finds how many items expire between 1st of this month and 1st of next month
        for (int m = 0; m < 12; m++)
        {
            DateOnly monthStart = firstMonth.AddMonths(m);
            DateOnly monthEnd = monthStart.AddMonths(1);

            // cancelled items are not counted
            int count = await items.CountAsync(i => i.LifecycleState != LifecycleState.Cancelled
                && i.CurrentPeriod!.ExpirationDate >= monthStart
                && i.CurrentPeriod!.ExpirationDate < monthEnd);

            result.Add(new MonthCount { Year = monthStart.Year, Month = monthStart.Month, Count = count });
        }
        return result;
    }


    // helper method for counting the items that are not cancelled and expired
    // grouped by how soon they expire
    public async Task<List<ChartEntry>> CountItemsByTimeLeft(IQueryable<Item> items, DateOnly today)
    {
        DateOnly in7 = today.AddDays(7);
        DateOnly in30 = today.AddDays(30);
        DateOnly in90 = today.AddDays(90);

        IQueryable<Item> upcoming = items.Where(i => i.LifecycleState != LifecycleState.Cancelled
                                        && i.CurrentPeriod!.ExpirationDate >= today);

        int week = await upcoming.CountAsync(i => i.CurrentPeriod!.ExpirationDate <= in7);
        int month = await upcoming.CountAsync(i => i.CurrentPeriod!.ExpirationDate > in7 && i.CurrentPeriod!.ExpirationDate <= in30);
        int quarter = await upcoming.CountAsync(i => i.CurrentPeriod!.ExpirationDate > in30 && i.CurrentPeriod!.ExpirationDate <= in90);
        int later = await upcoming.CountAsync(i => i.CurrentPeriod!.ExpirationDate > in90);

        return new List<ChartEntry>
        {
            new ChartEntry { Label = "7 days or less", Value = week },
            new ChartEntry { Label = "8-30 days", Value = month },
            new ChartEntry { Label = "31-90 days", Value = quarter },
            new ChartEntry { Label = "More than 90 days", Value = later }
        };
    }

    public async Task<AdminDashboardResponse> GetAdminDashboardAsync()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int active = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int totalItems = active + expiringSoon + expired + cancelled;

        // items per department
        List<DepartmentItemCount> byDepartment = await _db.Departments
            .OrderByDescending(d => d.Items.Count)
            .Select(d => new DepartmentItemCount
            {
                DepartmentName = d.Name,
                ItemCount = d.Items.Count
            })
            .ToListAsync();

        List<Item> expiringItems = await _db.Items
            .Where(i => i.LifecycleState == LifecycleState.ExpiringSoon)
            .Include(i => i.Department)
            .Include(i => i.CurrentPeriod)
            .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
            .Take(5)
            .ToListAsync();

        List<DashboardItem> upcomingExpirations = new List<DashboardItem>();
        foreach (Item item in expiringItems)
        {
            upcomingExpirations.Add(new DashboardItem
            {
                Id = item.Id,
                Name = item.Name,
                DepartmentName = item.Department.Name,
                ExpirationDate = item.CurrentPeriod!.ExpirationDate,
            });
        }
        return new AdminDashboardResponse
        {
            TotalItems = totalItems,
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            ItemsByDepartment = byDepartment,
            UpcomingExpirations = upcomingExpirations,
            ExpirationsByMonth = await CountExpirationsByMonth(_db.Items, today)
        };
    }

    public async Task<ManagerDashboardResponse> GetManagerDashboardAsync(int userId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        User user = await LoadCurrentUser(userId);

        int active = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items
            .Where(i => i.DepartmentId == user.DepartmentId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int totalItems = active + expiringSoon + expired + cancelled;

        // requests to review
        List<RenewalRequest> reviewRequests = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.PendingManagerApproval && r.Period.Item.DepartmentId == user.DepartmentId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsToReview = new List<DashboardRequest>();
        foreach (RenewalRequest reviewRequest in reviewRequests)
        {
            requestsToReview.Add(DashboardRequest.ToDashboardRequest(reviewRequest));
        }

        // requests to approve
        List<RenewalRequest> approveRequests = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.Approved && r.Period.Item.DepartmentId == user.DepartmentId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsToApprove = new List<DashboardRequest>();
        foreach (RenewalRequest approveRequest in approveRequests)
        {
            requestsToApprove.Add(DashboardRequest.ToDashboardRequest(approveRequest));
        }

        // upcoming expirations
        List<Item> expiringItems = await _db.Items
            .Where(i => i.LifecycleState == LifecycleState.ExpiringSoon && i.DepartmentId == user.DepartmentId)
            .Include(i => i.Department)
            .Include(i => i.CurrentPeriod)
            .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
            .Take(5)
            .ToListAsync();

        List<DashboardItem> upcomingExpirations = new List<DashboardItem>();
        foreach (Item item in expiringItems)
        {
            upcomingExpirations.Add(new DashboardItem
            {
                Id = item.Id,
                Name = item.Name,
                DepartmentName = item.Department.Name,
                ExpirationDate = item.CurrentPeriod!.ExpirationDate,
            });
        }

        List<ChartEntry> requestsByStatus = new List<ChartEntry>();
        foreach (RequestStatus status in Enum.GetValues<RequestStatus>())
        {
            int count = await _db.RenewalRequests
                  .CountAsync(r => r.Status == status && r.Period.Item.DepartmentId == user.DepartmentId);
            requestsByStatus.Add(new ChartEntry { Label = status.ToString(), Value = count });
        }

        return new ManagerDashboardResponse
        {
            TotalItems = totalItems,
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            RequestsToReview = requestsToReview,
            RequestsAwaitingCompletion = requestsToApprove,
            UpcomingExpirations = upcomingExpirations,
            ExpirationsByMonth = await CountExpirationsByMonth(_db.Items.Where(i => i.DepartmentId == user.DepartmentId), today),
            RequestsByStatus = requestsByStatus
        };
    }

    public async Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync(int userId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        User user = await LoadCurrentUser(userId);

        int active = await _db.Items
            .Where(i => i.ResponsibleUserId == userId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items
            .Where(i => i.ResponsibleUserId == userId)
            .CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items
            .Where(i => i.ResponsibleUserId == userId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items
            .Where(i => i.ResponsibleUserId == userId)
            .CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int totalItems = active + expiringSoon + expired + cancelled;

        // items needing renewal
        List<Item> needRenewal = await _db.Items
            .Where(i => (i.LifecycleState == LifecycleState.ExpiringSoon || i.LifecycleState == LifecycleState.Expired)
                        && i.ResponsibleUserId == userId
                        && !_db.RenewalRequests.Any(r => r.PeriodId == i.CurrentPeriodId
                            && r.Status != RequestStatus.Completed
                            && r.Status != RequestStatus.Rejected))
            .Include(i => i.Department)
            .Include(i => i.CurrentPeriod)
            .ToListAsync();

        List<DashboardItem> itemsNeedingRenewal = new List<DashboardItem>();
        foreach (Item item in needRenewal)
        {
            itemsNeedingRenewal.Add(new DashboardItem
            {
                Id = item.Id,
                Name = item.Name,
                DepartmentName = item.Department.Name,
                ExpirationDate = item.CurrentPeriod!.ExpirationDate,
            });
        }

        // requests needing clarification
        List<RenewalRequest> needClarification = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.ClarificationRequired && r.Period.Item.ResponsibleUserId == userId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsNeedingClarification = new List<DashboardRequest>();
        foreach (RenewalRequest reviewRequest in needClarification)
        {
            requestsNeedingClarification.Add(DashboardRequest.ToDashboardRequest(reviewRequest));
        }

        // requests awaiting completion
        List<RenewalRequest> awaitingCompletion = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.Approved && r.Period.Item.ResponsibleUserId == userId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsAwaitingCompletion = new List<DashboardRequest>();
        foreach (RenewalRequest request in awaitingCompletion)
        {
            requestsAwaitingCompletion.Add(DashboardRequest.ToDashboardRequest(request));
        }

        // upcoming expirations
        List<Item> expiringItems = await _db.Items
            .Where(i => i.LifecycleState == LifecycleState.ExpiringSoon && i.ResponsibleUserId == userId)
            .Include(i => i.Department)
            .Include(i => i.CurrentPeriod)
            .OrderBy(i => i.CurrentPeriod!.ExpirationDate)
            .Take(5)
            .ToListAsync();

        List<DashboardItem> upcomingExpirations = new List<DashboardItem>();
        foreach (Item item in expiringItems)
        {
            upcomingExpirations.Add(new DashboardItem
            {
                Id = item.Id,
                Name = item.Name,
                DepartmentName = item.Department.Name,
                ExpirationDate = item.CurrentPeriod!.ExpirationDate,
            });
        }

        // open requests
        List<RenewalRequest> openRequests = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status != RequestStatus.Completed && r.Status != RequestStatus.Rejected && r.Period.Item.ResponsibleUserId == userId)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> requestsOpen = new List<DashboardRequest>();
        foreach (RenewalRequest request in openRequests)
        {
            requestsOpen.Add(DashboardRequest.ToDashboardRequest(request));
        }


        return new EmployeeDashboardResponse
        {
            TotalItems = totalItems,
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            ItemsNeedingRenewal = itemsNeedingRenewal,
            RequestsNeedingClarification = requestsNeedingClarification,
            RequestsAwaitingCompletion = requestsAwaitingCompletion,
            MyOpenRequests = requestsOpen,
            UpcomingExpirations = upcomingExpirations,
            ExpirationsByMonth = await CountExpirationsByMonth(_db.Items.Where(i => i.ResponsibleUserId == userId), today),
            ItemsByTimeLeft = await CountItemsByTimeLeft(_db.Items.Where(i => i.ResponsibleUserId == userId), today)
        };
    }

    public async Task<FinanceDashboardResponse> GetFinanceDashboardAsync()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int active = await _db.Items
            .Where(i => i.CurrentPeriod!.Cost != null)
            .CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items
            .Where(i => i.CurrentPeriod!.Cost != null)
            .CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items
            .Where(i => i.CurrentPeriod!.Cost != null)
            .CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items
            .Where(i => i.CurrentPeriod!.Cost != null)
            .CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int requestsToReview = await _db.RenewalRequests
            .Where(r => r.Status == RequestStatus.PendingFinancialReview)
            .CountAsync();

        decimal totalCostWaitingForReview = await _db.RenewalRequests
            .Where(r => r.Status == RequestStatus.PendingFinancialReview && r.ProposedCost != null)
            .SumAsync(p => p.ProposedCost) ?? 0;

        decimal expectedUpcomingCosts = await _db.Items
            .Where(i => i.LifecycleState == LifecycleState.ExpiringSoon && i.CurrentPeriod!.Cost != null)
            .SumAsync(i => i.CurrentPeriod!.Cost) ?? 0;

        decimal approvedCost = await _db.RenewalRequests
            .Where(r => r.Status == RequestStatus.Approved && r.ProposedCost != null && r.NeedsFinancialReview)
            .SumAsync(r => r.ProposedCost) ?? 0;

        // requests needing financial review
        List<RenewalRequest> requestsNeedingFinancialReview = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == RequestStatus.PendingFinancialReview)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        List<DashboardRequest> financialRequests = new List<DashboardRequest>();
        foreach (RenewalRequest request in requestsNeedingFinancialReview)
        {
            financialRequests.Add(DashboardRequest.ToDashboardRequest(request));
        }

        List<ChartEntry> costsByDepartment = await _db.Departments
            .Select(d => new ChartEntry
            {
                Label = d.Name,
                Value = d.Items
                    .Where(i => i.LifecycleState != LifecycleState.Cancelled && i.CurrentPeriod!.Cost != null)
                    .Sum(i => i.CurrentPeriod!.Cost) ?? 0
            })
            .ToListAsync();

        return new FinanceDashboardResponse
        {
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            RequestsToReviewCount = requestsToReview,
            TotalCostWaitingForReview = totalCostWaitingForReview,
            ExpectedUpcomingCosts = expectedUpcomingCosts,
            ApprovedCost = approvedCost,
            RequestsNeedingFinancialReview = financialRequests,
            ExpirationsByMonth = await CountExpirationsByMonth(_db.Items.Where(i => i.CurrentPeriod!.Cost != null), today),
            CostsByDepartment = costsByDepartment.OrderByDescending(e => e.Value).ToList()
        };
    }
}