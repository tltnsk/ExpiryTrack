// Current lifecycle state of an organizational item

namespace ExpiryTrack.Api.Models.Enums;

public enum LifecycleState
{
    Active,
    ExpiringSoon,
    Expired,
    Cancelled
}