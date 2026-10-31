namespace ExpiryTrack.Api.DTO;

public class NotificationResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ItemId { get; set; }

    public int? RequestId { get; set; }
}
