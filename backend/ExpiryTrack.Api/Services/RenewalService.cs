using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpiryTrack.Api.Services;

public class RenewalService
{
    private readonly AppDbContext _db;

    public RenewalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RenewalResult> SubmitAsync(int itemId, int userId, SubmitRenewalRequest data)
    {
        Item? item = await _db.Items.Where(i => i.Id == itemId)
             .Include(i => i.CurrentPeriod)
             .Include(i => i.Department)
             .Include(i => i.Category)
             .FirstOrDefaultAsync();

        if (item == null || userId != item.ResponsibleUserId)
        {
            return new RenewalResult { NotFound = true };
        }

        if (item.LifecycleState != LifecycleState.ExpiringSoon)
        {
            return new RenewalResult { Error = $"{item.Name} is not expiring soon." };
        }

        bool openRequest = await _db.RenewalRequests
            .Where(r => r.PeriodId == item.CurrentPeriodId)
            .Where(r => r.Status != RequestStatus.Completed && r.Status != RequestStatus.Rejected)
            .AnyAsync();

        if (openRequest)
        {
            return new RenewalResult { Error = $"{item.Name} already has an open renewal request." };
        }

        if (data.ProposedExpirationDate <= item.CurrentPeriod!.ExpirationDate)
        {
            return new RenewalResult { Error = "Invalid proposed expiration date." };
        }

        if (item.Department.ManagerId == null)
        {
            return new RenewalResult { Error = "There is no department manager to forward the request to." };
        }

        var needsReview = data.ProposedCost != null && item.Category.RequiresFinancialReview &&
                          (item.Category.FinanceReviewThreshold == null ||
                           data.ProposedCost >= item.Category.FinanceReviewThreshold);

        DateTime now = DateTime.UtcNow;

        RenewalRequest request = new RenewalRequest
        {
            Status = needsReview ? RequestStatus.PendingFinancialReview : RequestStatus.PendingManagerApproval,
            NeedsFinancialReview = needsReview,
            ProposedExpirationDate = data.ProposedExpirationDate,
            ProposedCost = data.ProposedCost,
            Justification = data.Justification,
            PeriodId = item.CurrentPeriodId!.Value,
            RequestedByUserId = userId,
            SubmittedAt = now,
        };

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.Submitted,
            FromStatus = null,
            ToStatus = needsReview ? RequestStatus.PendingFinancialReview : RequestStatus.PendingManagerApproval,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);
        _db.RenewalRequests.Add(request);

        if (needsReview)
        {
            List<User> financeOfficers = await _db.Users
                .Where(u => u.Role.Name == "FinanceOfficer")
                .Where(u => u.IsActive)
                .ToListAsync();

            foreach (var financeOfficer in financeOfficers)
            {
                Notification notification = new Notification()
                {
                    Type = "RequestSubmitted",
                    Message = $"{item.Name} is expiring soon.",
                    Request = request,
                    RecipientUserId = financeOfficer.Id,
                    CreatedAt = now,
                };
                _db.Notifications.Add(notification);
            }
        }
        else
        {
            Notification notification = new Notification
            {
                Type = "RequestSubmitted",
                Message = $"{item.Name} is expiring soon.",
                Request = request,
                RecipientUserId = item.Department.ManagerId.Value,
                CreatedAt = now,
            };
            _db.Notifications.Add(notification);
        }

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }

    public async Task<RenewalResult> FinanceReviewAsync(int requestId, int userId, FinanceReviewRequest data)
    {
        RenewalRequest? request = await _db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .ThenInclude(i => i.Department)
            .SingleOrDefaultAsync(r => r.Id == requestId);

        if (request == null || !request.NeedsFinancialReview)
        {
            return new RenewalResult { NotFound = true };
        }

        if (request.Status != RequestStatus.PendingFinancialReview)
        {
            return new RenewalResult { Error = "This request has already been processed." };
        }

        request.Status = RequestStatus.PendingManagerApproval;
        DateTime now = DateTime.UtcNow;

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.FinanceReviewed,
            FromStatus = RequestStatus.PendingFinancialReview,
            ToStatus = RequestStatus.PendingManagerApproval,
            FinancialRecommendation = data.Recommendation,
            Comment = data.Comment,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);

        if (request.Period.Item.Department.ManagerId != null)
        {
            Notification notification = new Notification()
            {
                Type = "RequestReviewed",
                Message = $"{request.Period.Item.Name} is expiring soon.",
                Request = request,
                RecipientUserId = request.Period.Item.Department.ManagerId.Value,
                CreatedAt = now,
            };

            _db.Notifications.Add(notification);
        }

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }

    public async Task<RenewalResult> ApproveAsync(int requestId, int userId, ManagerDecisionRequest data)
    {
        RenewalRequest? request = await _db.RenewalRequests.Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .ThenInclude(i => i.Department)
            .SingleOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Period.Item.Department.ManagerId != userId)
        {
            return new RenewalResult { NotFound = true };
        }

        if (request.Status != RequestStatus.PendingManagerApproval)
        {
            return new RenewalResult { Error = "This request has already been processed." };
        }

        request.Status = RequestStatus.Approved;
        DateTime now = DateTime.UtcNow;
        request.DecidedAt = now;

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.Approved,
            FromStatus = RequestStatus.PendingManagerApproval,
            ToStatus = RequestStatus.Approved,
            Comment = data.Comment,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);

        Notification notification = new Notification()
        {
            Type = "RequestApproved",
            Message = $"{request.Period.Item.Name} is approved for renewal.",
            Request = request,
            RecipientUserId = request.Period.Item.ResponsibleUserId,
            CreatedAt = now,
        };

        _db.Notifications.Add(notification);

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }

    public async Task<RenewalResult> RejectAsync(int requestId, int userId, ManagerDecisionRequest data)
    {
        RenewalRequest? request = await _db.RenewalRequests.Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .ThenInclude(i => i.Department)
            .SingleOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Period.Item.Department.ManagerId != userId)
        {
            return new RenewalResult { NotFound = true };
        }

        // the manager can reject the renewal request also when waiting for a clarification from the employee
        if (request.Status != RequestStatus.PendingManagerApproval && request.Status != RequestStatus.ClarificationRequired)
        {
            return new RenewalResult { Error = "Cannot process the renewal request." };
        }

        if (string.IsNullOrWhiteSpace(data.Comment))
        {
            return new RenewalResult { Error = "Reason is required when rejecting a request." };
        }

        RequestStatus oldStatus = request.Status;
        request.Status = RequestStatus.Rejected;
        DateTime now = DateTime.UtcNow;
        request.DecidedAt = now;

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.Rejected,
            FromStatus = oldStatus,
            ToStatus = RequestStatus.Rejected,
            Comment = data.Comment,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);

        Notification notification = new Notification()
        {
            Type = "RequestRejected",
            Message = $"{request.Period.Item.Name} is rejected by the manager.",
            Request = request,
            RecipientUserId = request.Period.Item.ResponsibleUserId,
            CreatedAt = now,
        };

        _db.Notifications.Add(notification);

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }

    public async Task<RenewalResult> ClarificationAsync(int requestId, int userId, ManagerDecisionRequest data)
    {
        RenewalRequest? request = await _db.RenewalRequests.Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .ThenInclude(i => i.Department)
            .SingleOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Period.Item.Department.ManagerId != userId)
        {
            return new RenewalResult { NotFound = true };
        }

        // managers can only request one clarification for the renewal request 
        RequestAction? requestAction = await _db.RequestActions.Include(r => r.Request)
            .Where(r => r.ActionType == RequestActionType.ClarificationRequested)
            .SingleOrDefaultAsync(r => r.RequestId == requestId);

        if (request.Status != RequestStatus.PendingManagerApproval)
        {
            return new RenewalResult { Error = "This request has already been processed." };
        }

        if (requestAction != null)
        {
            return new RenewalResult { Error = "Clarification has already been requested for this renewal request." };
        }

        if (string.IsNullOrWhiteSpace(data.Comment))
        {
            return new RenewalResult { Error = "Must provide a question for clarification." };
        }

        request.Status = RequestStatus.ClarificationRequired;
        DateTime now = DateTime.UtcNow;

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.ClarificationRequested,
            FromStatus = RequestStatus.PendingManagerApproval,
            ToStatus = RequestStatus.ClarificationRequired,
            Comment = data.Comment,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);

        Notification notification = new Notification()
        {
            Type = "ClarificationRequested",
            Message = $"Manager required clarification for {request.Period.Item.Name} renewal request.",
            Request = request,
            RecipientUserId = request.Period.Item.ResponsibleUserId,
            CreatedAt = now,
        };

        _db.Notifications.Add(notification);

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }

    public async Task<RenewalResult> AnswerClarificationAsync(int requestId, int userId, ClarificationAnswerRequest data)
    {
        RenewalRequest? request = await _db.RenewalRequests.Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .ThenInclude(i => i.Department)
            .SingleOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Period.Item.ResponsibleUserId != userId)
        {
            return new RenewalResult { NotFound = true };
        }

        if (request.Status != RequestStatus.ClarificationRequired)
        {
            return new RenewalResult { Error = "This request has already been processed." };
        }

        if (string.IsNullOrWhiteSpace(data.Answer))
        {
            return new RenewalResult { Error = "Must provide an answer for clarification." };
        }

        request.Status = RequestStatus.PendingManagerApproval;
        DateTime now = DateTime.UtcNow;

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.ClarificationProvided,
            FromStatus = RequestStatus.ClarificationRequired,
            ToStatus = RequestStatus.PendingManagerApproval,
            Comment = data.Answer,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);

        if (request.Period.Item.Department.ManagerId != null)
        {
            Notification notification = new Notification()
            {
                Type = "ClarificationProvided",
                Message = $"Employee provided clarification for {request.Period.Item.Name}.",
                Request = request,
                RecipientUserId = request.Period.Item.Department.ManagerId.Value,
                CreatedAt = now,
            };

            _db.Notifications.Add(notification);
        }

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }

    public async Task<RenewalResult> CompleteAsync(int requestId, int userId)
    {
        DateTime now = DateTime.UtcNow;
        // get today's date 
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // include the category as it is needed for setting the lifecycle state after the renewal is completed 
        RenewalRequest? request = await _db.RenewalRequests.
            Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .ThenInclude(i => i.Category)
            .Include(r => r.Period)
                .ThenInclude(p => p.Item)
                .ThenInclude(i => i.Department)
            .SingleOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Period.Item.ResponsibleUserId != userId)
        {
            return new RenewalResult { NotFound = true };
        }

        if (request.Status != RequestStatus.Approved)
        {
            return new RenewalResult { Error = "This request has not been approved or has already been processed." };
        }

        // the new period must be valid
        // the new expiration date must be after today
        if (request.ProposedExpirationDate <= today)
        {
            return new RenewalResult { Error = "The proposed data must be later than today." };
        }

        request.Status = RequestStatus.Completed;

        request.CompletedAt = now;
        // remember the old state 
        LifecycleState oldState = request.Period.Item.LifecycleState;

        // calculate how many days are left until expiration
        int daysLeft = request.ProposedExpirationDate.DayNumber - today.DayNumber;
        // find the new state based on the days left until expiration and the item's warning period days
        LifecycleState newState = daysLeft <= request.Period.Item.Category.WarningPeriodDays
            ? LifecycleState.ExpiringSoon
            : LifecycleState.Active;

        RequestAction action = new RequestAction
        {
            ActionType = RequestActionType.Completed,
            FromStatus = RequestStatus.Approved,
            ToStatus = RequestStatus.Completed,
            ActorUserId = userId,
            CreatedAt = now,
        };

        request.Actions.Add(action);

        // send a notification to the department manager that the renewal has been completed 
        if (request.Period.Item.Department.ManagerId != null)
        {
            Notification notification = new Notification()
            {
                Type = "RenewalCompleted",
                Message = $"Renewal for {request.Period.Item.Name} has been completed.",
                Request = request,
                RecipientUserId = request.Period.Item.Department.ManagerId.Value,
                CreatedAt = now,
            };
            _db.Notifications.Add(notification);
        }

        LifecyclePeriod period = new LifecyclePeriod()
        {
            PeriodNumber = request.Period.PeriodNumber + 1,
            StartDate = today,
            ExpirationDate = request.ProposedExpirationDate,
            Cost = request.ProposedCost,
            CreatedAt = now,
            ItemId = request.Period.Item.Id,
            CreatedFromRequestId = request.Id,
        };

        // add the period 
        request.Period.Item.Periods.Add(period);

        // set the item's period to the new period
        request.Period.Item.CurrentPeriod = period;

        // set item's EscalatedAt to null 
        request.Period.Item.EscalatedAt = null;

        // set the item's lifecycle state to the new state
        request.Period.Item.LifecycleState = newState;

        request.Period.Item.StateChangedAt = now;

        // add the record to item's history 
        ItemStateHistory history = new ItemStateHistory()
        {
            FromState = oldState,
            // state is set based on the number of days left until expiration 
            ToState = newState,
            ChangedAt = now,
            ItemId = request.Period.Item.Id,
            ChangedByUserId = userId,
            Reason = "Renewal completed"
        };

        _db.ItemStateHistory.Add(history);

        await _db.SaveChangesAsync();
        return new RenewalResult { Request = request };
    }
}