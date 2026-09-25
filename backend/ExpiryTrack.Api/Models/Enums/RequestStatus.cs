namespace ExpiryTrack.Api.Models.Enums;

public enum RequestStatus
{
    PendingFinancialReview,
    PendingManagerApproval,
    ClarificationRequired,
    Approved,
    Rejected,
    Completed
}