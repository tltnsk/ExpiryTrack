namespace ExpiryTrack.Api.Models;

// A PDF document attached to exactly one item or renewal request
public class Document
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";

    // file name used when storing the file 
    public string StoredFileName { get; set; } = "";
    public int SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    // ItemId and RequestId are nullable because a document doesn't belong to both
    public int? ItemId { get; set; }
    public Item? Item { get; set; }

    public int? RequestId { get; set; }
    public RenewalRequest? Request { get; set; }

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
}
