using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.Models;

public class RenewalRequest
{
    public int Id { get; set; }
    public RequestStatus Status { get; set; }

    // stores whether finance review was needed 
    // when the request was submitted
    public bool NeedsFinancialReview { get; set; }
    public DateOnly ProposedExpirationDate { get; set; }
    public decimal? ProposedCost { get; set; }
    public string? Justification { get; set; }
    public bool ClarificationUsed { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // used to ensure that a period has only one open renewal request
    public int? OpenPeriodId { get; private set; }

    // The period that is being renewed
    public int PeriodId { get; set; }
    public LifecyclePeriod Period { get; set; } = null!;

    // Employee who submitted the request
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    // The new period created when this request is completed
    public LifecyclePeriod? CreatedPeriod { get; set; }

    public ICollection<RequestAction> Actions { get; set; } = new List<RequestAction>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
