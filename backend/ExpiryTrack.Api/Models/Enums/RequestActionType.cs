// Represents an action which was recorded during the renewal request workflow

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