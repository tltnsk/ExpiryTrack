namespace ExpiryTrack.Api.DTO;

public class DocumentResponse
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";
    public int SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedByUserName { get; set; } = "";
}