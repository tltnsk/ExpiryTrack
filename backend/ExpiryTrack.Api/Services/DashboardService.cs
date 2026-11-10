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
    
    public async Task<AdminDashboardResponse> GetAdminDashboardAsync()
    {
        int active = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Active);
        int expiringSoon = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.ExpiringSoon);
        int expired = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Expired);
        int cancelled = await _db.Items.CountAsync(i => i.LifecycleState == LifecycleState.Cancelled);

        int totalItems = active + expiringSoon + expired + cancelled;

        // items per department
        List<DepartmentItemCount> byDepartment = await _db.Departments
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
            UpcomingExpirations = upcomingExpirations
        };
    }
    
    public async Task<ManagerDashboardResponse> GetManagerDashboardAsync(int userId)
    {
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
        return new ManagerDashboardResponse
        {
            TotalItems = totalItems,
            ActiveItems = active,
            ExpiringSoonItems = expiringSoon,
            ExpiredItems = expired,
            CancelledItems = cancelled,
            RequestsToReview = requestsToReview,
            RequestsAwaitingCompletion = requestsToApprove,
            UpcomingExpirations = upcomingExpirations
        };
    }
    
    public async Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync(int  userId)
    {
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
            .Where(r => r.Status == RequestStatus.ClarificationRequired && r.RequestedByUserId == userId)
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
            .Where(r => r.Status == RequestStatus.Approved && r.RequestedByUserId == userId)
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
            .Where(r => r.Status != RequestStatus.Completed && r.Status != RequestStatus.Rejected && r.RequestedByUserId == userId)
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
            UpcomingExpirations = upcomingExpirations
        };
    }
    
    public async Task<FinanceDashboardResponse> GetFinanceDashboardAsync()
    {
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

        return new FinanceDashboardResponse
        {
            RequestsToReviewCount = requestsToReview,
            TotalCostWaitingForReview = totalCostWaitingForReview,
            ExpectedUpcomingCosts = expectedUpcomingCosts,
            ApprovedCost = approvedCost,
            RequestsNeedingFinancialReview = financialRequests,
        };
    }
}