namespace ExpiryTrack.Api.Models;

// A PDF document attached to exactly one item or renewal request
public class Document
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";

    public string StoredFileName { get; set; } = "";
    public int SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public int? ItemId { get; set; }
    public Item? Item { get; set; }

    public int? RequestId { get; set; }
    public RenewalRequest? Request { get; set; }

    public int UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}
