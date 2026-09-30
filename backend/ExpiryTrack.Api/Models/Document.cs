namespace ExpiryTrack.Api.Models;

// a pdf document which can be attached to an item or a renewal request

public class Document
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";

    // file name stored on disk
    public string StoredFileName { get; set; } = "";
    public int SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int? ItemId { get; set; }
    public Item? Item { get; set; }
    public int? RequestId { get; set; }
    public RenewalRequest? Request { get; set; }

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
}