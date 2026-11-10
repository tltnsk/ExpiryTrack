using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.DTO;

public class DepartmentItemCount
{
    public string DepartmentName { get; set; } = "";
    public int ItemCount { get; set; }
}

public class DashboardItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public decimal? Cost { get; set; }
    public DateOnly ExpirationDate { get; set; }
}

public class AdminDashboardResponse
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int ExpiringSoonItems { get; set; }
    public int ExpiredItems { get; set; }
    public int CancelledItems { get; set; }
    public List<DepartmentItemCount> ItemsByDepartment { get; set; } = new();
    public List<DashboardItem> UpcomingExpirations { get; set; } = new();
}

// one row in a list of requests 
// used by several dashboards
public class DashboardRequest
{
    public int Id { get; set; }
    public string ItemName { get; set; } = "";
    public RequestStatus Status { get; set; }
    public bool NeedsFinancialReview { get; set; }
    public DateOnly ProposedExpirationDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public decimal? ProposedCost { get; set; }
    public string RequestedByUserName { get; set; } = "";

    public static DashboardRequest ToDashboardRequest(RenewalRequest renewalRequest)
    {
        return new DashboardRequest
        {
            Id = renewalRequest.Id,
            ItemName = renewalRequest.Period.Item.Name,
            Status = renewalRequest.Status,
            NeedsFinancialReview = renewalRequest.NeedsFinancialReview,
            ProposedExpirationDate = renewalRequest.ProposedExpirationDate,
            SubmittedAt = renewalRequest.SubmittedAt,
            ProposedCost = renewalRequest.ProposedCost,
            RequestedByUserName =
                renewalRequest.RequestedByUser.FirstName + " " + renewalRequest.RequestedByUser.LastName,
        };
    }
}

public class ManagerDashboardResponse
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int ExpiringSoonItems { get; set; }
    public int ExpiredItems { get; set; }
    public int CancelledItems { get; set; }

    public List<DashboardRequest> RequestsToReview { get; set; } = new();
    public List<DashboardRequest> RequestsAwaitingCompletion { get; set; } = new();
    public List<DashboardItem> UpcomingExpirations { get; set; } = new();
}

public class EmployeeDashboardResponse
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int ExpiringSoonItems { get; set; }
    public int ExpiredItems { get; set; }
    public int CancelledItems { get; set; }
    public List<DashboardItem> ItemsNeedingRenewal { get; set; } = new();
    public List<DashboardRequest> RequestsNeedingClarification { get; set; } = new();
    public List<DashboardRequest> RequestsAwaitingCompletion { get; set; } = new();
    public List<DashboardRequest> MyOpenRequests { get; set; } = new();
    public List<DashboardItem> UpcomingExpirations { get; set; } = new();
}

public class FinanceDashboardResponse
{
    public int RequestsToReviewCount { get; set; }
    public decimal TotalCostWaitingForReview { get; set; }
    public decimal ExpectedUpcomingCosts { get; set; }
    public decimal ApprovedCost { get; set; }
    public List<DashboardRequest> RequestsNeedingFinancialReview { get; set; } = new();
}