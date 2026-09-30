using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? ReferenceNumber { get; set; }
    public LifecycleState LifecycleState { get; set; }
    public DateTime StateChangedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int ResponsibleUserId { get; set; }
    public User ResponsibleUser { get; set; } = null!;

    // null only while the item and its first period are being created
    public int? CurrentPeriodId { get; set; }

    public LifecyclePeriod? CurrentPeriod { get; set; }

    // All validity periods of this item 
    public ICollection<LifecyclePeriod> Periods { get; set; } = new List<LifecyclePeriod>();

    public ICollection<ItemStateHistory> StateHistory { get; set; } = new List<ItemStateHistory>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
