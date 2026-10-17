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
            RequestedByUserId = request.RequestedByUserId
        };
    }
}
