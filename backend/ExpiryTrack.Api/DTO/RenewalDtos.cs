using System.ComponentModel.DataAnnotations;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.DTO;

public class SubmitRenewalRequest
{
    [Required]
    public DateOnly ProposedExpirationDate { get; set; }

    [Range(0.01, 9999999.99)]
    public decimal? ProposedCost { get; set; }
    [Required]
    public string? Justification { get; set; }
}

public class RenewalRequestResponse
{
    public int Id { get; set; }
    public RequestStatus Status { get; set; }
    public bool NeedsFinancialReview { get; set; }
    public DateOnly ProposedExpirationDate { get; set; }
    public decimal? ProposedCost { get; set; }
    public string? Justification { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PeriodId { get; set; }
    public int RequestedByUserId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";

    public List<RequestActionResponse> Actions { get; set; } = new();

    public static RenewalRequestResponse FromRequest(RenewalRequest request)
    {
        return new RenewalRequestResponse
        {
            Id = request.Id,
            Status = request.Status,
            NeedsFinancialReview = request.NeedsFinancialReview,
            ProposedExpirationDate = request.ProposedExpirationDate,
            ProposedCost = request.ProposedCost,
            Justification = request.Justification,
            SubmittedAt = request.SubmittedAt,
            DecidedAt = request.DecidedAt,
            CompletedAt = request.CompletedAt,
            PeriodId = request.PeriodId,
            RequestedByUserId = request.RequestedByUserId,
            ItemId = request.Period.ItemId,
            ItemName = request.Period.Item.Name,
            Actions = request.Actions
                .OrderBy(a => a.CreatedAt)
                .Select(a => RequestActionResponse.FromAction(a))
                .ToList(),
        };
    }
}

// what the finance officer returns for a request
public class FinanceReviewRequest
{
    [Required]
    public FinancialRecommendation? Recommendation { get; set; }
    [Required]
    public string? Comment { get; set; }
}

// what the manager returns for a request
public class ManagerDecisionRequest
{
    public string? Comment { get; set; }
}

// what the employee sends for a clarification
public class ClarificationAnswerRequest
{
    [Required]
    public string? Answer { get; set; }
}

// what the employee sends after completing renewal
public class CompleteRenewalRequest
{
    [Range(0.01, 9999999.99)]
    public decimal? ActualCost { get; set; }
}

// comments in history
public class RequestActionResponse
{
    public RequestActionType Type { get; set; }
    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public string? Comment { get; set; }
    public FinancialRecommendation? Recommendation { get; set; }
    public int? ActorUserId { get; set; }
    public string ActorUserName { get; set; } = "System";
    public DateTime CreatedAt { get; set; }

    public static RequestActionResponse FromAction(RequestAction action)
    {
        return new RequestActionResponse
        {
            Type = action.ActionType,
            FromStatus = action.FromStatus,
            ToStatus = action.ToStatus,
            Recommendation = action.FinancialRecommendation,
            Comment = action.Comment,
            ActorUserId = action.ActorUserId,
            ActorUserName = action.ActorUser != null ? action.ActorUser.FirstName + " " + action.ActorUser.LastName : "System",
            CreatedAt = action.CreatedAt,
        };
    }
}