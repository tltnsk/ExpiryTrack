using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Api.Services;

public class ExpirationService
{
    private readonly AppDbContext _db;

    public ExpirationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RunCheckAsync(DateOnly today)
    {
        List<Item> items = await _db.Items.Where(i =>
            i.LifecycleState.Equals(LifecycleState.Active) || i.LifecycleState.Equals(LifecycleState.ExpiringSoon))
            .Include(i => i.CurrentPeriod)
            .Include(i => i.Category)
            .Include(i => i.Department)
            .ToListAsync();


        foreach (var item in items)
        {
            int daysLeft = item.CurrentPeriod!.ExpirationDate.DayNumber - today.DayNumber;
            if (daysLeft < 0)
            // needs to be daysLeft < 0 so that the system can catch up after not being open for a while
            {
                var now = DateTime.UtcNow;
                var oldState = item.LifecycleState;
                item.LifecycleState = LifecycleState.Expired;
                item.StateChangedAt = now;

                // add a new state history for the item 
                _db.ItemStateHistory.Add(new ItemStateHistory
                {
                    ItemId = item.Id,
                    FromState = oldState,
                    ToState = LifecycleState.Expired,
                    ChangedByUserId = null,
                    Reason = "Item expired without being renewed.",
                    ChangedAt = now,
                }
                );

                RenewalRequest? request = await _db.RenewalRequests
                    .Where(r => r.PeriodId == item.CurrentPeriodId)
                    .Where(r => r.Status == RequestStatus.ClarificationRequired
                                || r.Status == RequestStatus.PendingFinancialReview ||
                                r.Status == RequestStatus.PendingManagerApproval)
                    .FirstOrDefaultAsync();


                // if the renewal request hasn't been approved and the item expired, reject the renewal request
                if (request != null)
                {
                    var oldStatus = request.Status;
                    request.Status = RequestStatus.Rejected;
                    request.DecidedAt = now;
                    _db.RequestActions.Add(new RequestAction
                    {
                        RequestId = request.Id,
                        ActionType = RequestActionType.ClosedByExpiry,
                        FromStatus = oldStatus,
                        ToStatus = RequestStatus.Rejected,
                        ActorUserId = null,
                        Comment = "Item expired before renewal was completed",
                        CreatedAt = now,
                    });
                }

                // notify responsible employee and department manager for the expiration
                _db.Notifications.Add(new Notification
                {
                    Type = "ItemExpired",
                    Message = $"{item.Name} expired on {item.CurrentPeriod.ExpirationDate}.",
                    IsRead = false,
                    RecipientUserId = item.ResponsibleUserId,
                    RequestId = null,
                    ItemId = item.Id,
                    CreatedAt = now
                });

                if (item.Department.ManagerId != null)
                {
                    _db.Notifications.Add(new Notification
                    {
                        Type = "ItemExpired",
                        Message = $"{item.Name} expired on {item.CurrentPeriod.ExpirationDate}.",
                        IsRead = false,
                        RecipientUserId = item.Department.ManagerId.Value,
                        RequestId = null,
                        ItemId = item.Id,
                        CreatedAt = now
                    });
                }

                await _db.SaveChangesAsync();
            }
            else if (item.LifecycleState.Equals(LifecycleState.Active) && daysLeft <= item.Category.WarningPeriodDays)
            {
                var now = DateTime.UtcNow;

                item.LifecycleState = LifecycleState.ExpiringSoon;
                item.StateChangedAt = now;

                _db.ItemStateHistory.Add(new ItemStateHistory
                {
                    ItemId = item.Id,
                    FromState = LifecycleState.Active,
                    ToState = LifecycleState.ExpiringSoon,
                    ChangedByUserId = null,
                    Reason = "Item expiring soon.",
                    ChangedAt = now,
                });

                _db.Notifications.Add(new Notification
                {
                    Type = "ItemExpiringSoon",
                    Message = $"{item.Name} expiring soon.",
                    IsRead = false,
                    RecipientUserId = item.ResponsibleUserId,
                    RequestId = null,
                    ItemId = item.Id,
                    CreatedAt = now
                });

                await _db.SaveChangesAsync();
            }
            else if (item.LifecycleState == LifecycleState.ExpiringSoon && item.EscalatedAt == null &&
                     daysLeft <= item.Category.WarningPeriodDays / 2)
            {
                bool openRequest = await _db.RenewalRequests
                        .Where(r => r.PeriodId == item.CurrentPeriodId)
                        .Where(r => r.Status != RequestStatus.Rejected && r.Status != RequestStatus.Completed)
                        .AnyAsync();

                // if the employee hasn't submitted a renewal requests for the item 
                // and half of the renewal period has passed notify the manager
                if (!openRequest && item.Department.ManagerId != null)
                {
                    DateTime now = DateTime.UtcNow;
                    _db.Notifications.Add(new Notification
                    {
                        Type = "Escalation",
                        Message = $"Item {item.Name} expiring soon {item.CurrentPeriod.ExpirationDate} and no renewal request has been submitted yet.",
                        IsRead = false,
                        RecipientUserId = item.Department.ManagerId.Value,
                        RequestId = null,
                        ItemId = item.Id,
                        CreatedAt = now
                    });

                    item.EscalatedAt = now;
                }

                await _db.SaveChangesAsync();
            }
        }
    }
}