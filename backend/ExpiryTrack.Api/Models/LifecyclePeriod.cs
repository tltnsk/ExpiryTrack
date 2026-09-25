namespace ExpiryTrack.Api.Models;

public class LifecyclePeriod
{
    public int Id { get; set; }
    public int PeriodNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public decimal? Cost { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    // The renewal request that created this period
    // null for the first period
    public int? CreatedFromRequestId { get; set; }
    public RenewalRequest? CreatedFromRequest { get; set; }

    // Renewal requests made for this period
    public ICollection<RenewalRequest> RenewalRequests { get; set; } = new List<RenewalRequest>();
}
