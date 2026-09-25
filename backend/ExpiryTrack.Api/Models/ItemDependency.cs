namespace ExpiryTrack.Api.Models;

// "Item depends on DependsOnItem". Primary key is (ItemId, DependsOnItemId).
public class ItemDependency
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int DependsOnItemId { get; set; }
    public Item DependsOnItem { get; set; } = null!;
}
