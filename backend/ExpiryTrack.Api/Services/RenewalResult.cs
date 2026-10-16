// used for communication between service and controller

// communication is: frontend->controller->service->controller->frontend

using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Services;

public class RenewalResult
{
    public RenewalRequest? Request { get; set; }

    // why the action was refused, e.g. the item is not expiring soon, there is an open requeset already, no manager etc.
    public string? Error { get; set; }

    // item missing
    public bool NotFound { get; set; }
}