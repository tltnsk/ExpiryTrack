namespace PrivateExpiryTrack.Api.Models;

// In-app notification, linked to at most one item or one request
public class Notification
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public int RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    // related item 
    public int? ItemId { get; set; }
    public Item? Item { get; set; }

    public int? RequestId { get; set; }
    public RenewalRequest? Request { get; set; }
}
