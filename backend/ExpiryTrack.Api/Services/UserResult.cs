using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Services;

public class UserResult
{
    public User? User { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public string? Conflict { get; set; }
}