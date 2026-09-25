using ExpiryTrack.Api.Models.Enums;

// Stores the history of an item's lifecycle state changes
namespace ExpiryTrack.Api.Models;

public class ItemStateHistory
{
    public int Id { get; set; }

    // null when the item is created
    public LifecycleState? FromState { get; set; }
    public LifecycleState ToState { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;


    public int? ChangedById { get; set; }
    public User? ChangedBy { get; set; }
}
