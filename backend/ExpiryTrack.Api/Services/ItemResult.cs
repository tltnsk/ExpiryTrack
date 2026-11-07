using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Services;

public class ItemResult
{
    // the created item
    public Item? Item { get; set; }
    public string? Error { get; set; }
    // errors in rows
    public List<string> RowErrors { get; set; } = new();
    // how many items were imported
    public int CreatedCount { get; set; }
}