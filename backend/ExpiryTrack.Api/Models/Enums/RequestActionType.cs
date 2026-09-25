namespace ExpiryTrack.Api.Models.Enums;

public enum RequestActionType
{
    Submitted,
    FinanceReviewed,
    ClarificationRequested,
    ClarificationProvided,
    Approved,
    Rejected,
    Completed,
    ClosedByExpiry
}