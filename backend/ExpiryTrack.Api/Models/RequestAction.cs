using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.Models;

public class RequestAction
{
    public int Id { get; set; }
    public RequestActionType ActionType { get; set; }

    // null for the first action
    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }

    // Set only by finance officers
    public FinancialRecommendation? Recommendation { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public int RequestId { get; set; }
    public RenewalRequest Request { get; set; } = null!;

    // NULL = system (daily expiry job)
    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
}
