using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpiryTrack.Api.Data.Seeders;

public static class RenewalRequestSeeder
{
    public static async Task SeedAsync(AppDbContext db, List<Item> demoItems)
    {
        var categories = await db.Categories.ToDictionaryAsync(c => c.Id);
        var employee = await db.Users.SingleAsync(u => u.Email == "employee@test.com");
        var manager = await db.Users.SingleAsync(u => u.Email == "manager@test.com");
        var finance = await db.Users.SingleAsync(u => u.Email == "finance@test.com");
        var now = DateTime.UtcNow;

        // waiting for the manager 
        var microsoft = FindItem(demoItems, "Microsoft 365 Business");
        Submit(db, microsoft, categories[microsoft.CategoryId], employee, manager, finance, 2500,
            "All 20 licences are still used by the IT team.", now.AddDays(-1));

        // waiting for finance (leases need a financial review)
        var lease = FindItem(demoItems, "Office lease Sofia HQ");
        Submit(db, lease, categories[lease.CategoryId], employee, manager, finance, 38000,
            "The rent increases by about 5% from next year.", now.AddDays(-2));

        // clarification required: finance reviewed it, then the manager asked a question
        var insurance = FindItem(demoItems, "Company car insurance");
        var insuranceRequest = Submit(db, insurance, categories[insurance.CategoryId], employee, manager, finance, 1950,
            "Same coverage as this year.", now.AddDays(-6));
        FinanceReview(db, insuranceRequest, insurance, finance, manager, FinancialRecommendation.Approve,
            "The price is in line with last year.", now.AddDays(-4));
        RequestClarification(db, insuranceRequest, insurance, manager,
            "Did you compare offers from other insurers?", now.AddDays(-2));

        // approved, so the employee can complete it 
        var ssl = FindItem(demoItems, "SSL certificate for company website");
        var sslRequest = Submit(db, ssl, categories[ssl.CategoryId], employee, manager, finance, 300,
            "The website is not reachable over HTTPS without the certificate.", now.AddDays(-3));
        Approve(db, sslRequest, ssl, manager, "Approved, please renew it this week.", now.AddDays(-2));

        // rejected before the item expired
        var backup = FindItem(demoItems, "Cloud backup service");
        var backupRequest = Submit(db, backup, categories[backup.CategoryId], employee, manager, finance, 1500,
            "Backups of the file server.", now.AddDays(-20));
        Reject(db, backupRequest, backup, manager, "The backups are moving to the new server provider.", now.AddDays(-18));

        await db.SaveChangesAsync();
    }

    private static Item FindItem(List<Item> demoItems, string name)
    {
        return demoItems.Single(i => i.Name == name);
    }

    // creates a submitted request
    private static RenewalRequest Submit(AppDbContext db, Item item, Category category, User employee, User manager, User finance,
        decimal cost, string justification, DateTime submittedAt)
    {
        // the same rule as the application
        if (item.LifecycleState != LifecycleState.ExpiringSoon && item.LifecycleState != LifecycleState.Expired)
            throw new InvalidOperationException($"{item.Name} must be expiring soon or expired to get a renewal request.");

        bool needsReview = category.RequiresFinancialReview &&
                           (category.FinanceReviewThreshold == null || cost >= category.FinanceReviewThreshold);
        var status = needsReview ? RequestStatus.PendingFinancialReview : RequestStatus.PendingManagerApproval;

        var request = new RenewalRequest
        {
            Status = status,
            NeedsFinancialReview = needsReview,
            ProposedExpirationDate = item.CurrentPeriod!.ExpirationDate.AddYears(1),
            ProposedCost = cost,
            Justification = justification,
            PeriodId = item.CurrentPeriod.Id,
            RequestedByUserId = employee.Id,
            SubmittedAt = submittedAt,
        };

        request.Actions.Add(new RequestAction
        {
            ActionType = RequestActionType.Submitted,
            FromStatus = null,
            ToStatus = status,
            ActorUserId = employee.Id,
            CreatedAt = submittedAt,
        });
        db.RenewalRequests.Add(request);

        if (needsReview)
        {
            Notify(db, request, finance.Id, "RequestSubmitted",
                $"A renewal request for {item.Name} was submitted and needs financial review.", submittedAt);
        }
        else
        {
            Notify(db, request, manager.Id, "RequestSubmitted",
                $"A renewal request for {item.Name} was submitted and needs your decision.", submittedAt);
        }

        return request;
    }

    private static void FinanceReview(AppDbContext db, RenewalRequest request, Item item, User finance, User manager,
        FinancialRecommendation recommendation, string comment, DateTime reviewedAt)
    {
        CheckStatus(request, RequestStatus.PendingFinancialReview);
        request.Status = RequestStatus.PendingManagerApproval;

        request.Actions.Add(new RequestAction
        {
            ActionType = RequestActionType.FinanceReviewed,
            FromStatus = RequestStatus.PendingFinancialReview,
            ToStatus = RequestStatus.PendingManagerApproval,
            FinancialRecommendation = recommendation,
            Comment = comment,
            ActorUserId = finance.Id,
            CreatedAt = reviewedAt,
        });

        Notify(db, request, manager.Id, "RequestReviewed",
            $"{item.Name} has been reviewed by finance and needs your decision.", reviewedAt);
    }

    private static void RequestClarification(AppDbContext db, RenewalRequest request, Item item, User manager,
        string question, DateTime askedAt)
    {
        CheckStatus(request, RequestStatus.PendingManagerApproval);
        request.Status = RequestStatus.ClarificationRequired;

        request.Actions.Add(new RequestAction
        {
            ActionType = RequestActionType.ClarificationRequested,
            FromStatus = RequestStatus.PendingManagerApproval,
            ToStatus = RequestStatus.ClarificationRequired,
            Comment = question,
            ActorUserId = manager.Id,
            CreatedAt = askedAt,
        });

        Notify(db, request, item.ResponsibleUserId, "ClarificationRequested",
            $"The manager requested clarification for the renewal request for {item.Name}.", askedAt);
    }

    private static void Approve(AppDbContext db, RenewalRequest request, Item item, User manager, string comment, DateTime decidedAt)
    {
        CheckStatus(request, RequestStatus.PendingManagerApproval);
        request.Status = RequestStatus.Approved;
        request.DecidedAt = decidedAt;

        request.Actions.Add(new RequestAction
        {
            ActionType = RequestActionType.Approved,
            FromStatus = RequestStatus.PendingManagerApproval,
            ToStatus = RequestStatus.Approved,
            Comment = comment,
            ActorUserId = manager.Id,
            CreatedAt = decidedAt,
        });

        Notify(db, request, item.ResponsibleUserId, "RequestApproved",
            $"The renewal request for {item.Name} was approved.", decidedAt);
    }

    private static void Reject(AppDbContext db, RenewalRequest request, Item item, User manager, string reason, DateTime decidedAt)
    {
        CheckStatus(request, RequestStatus.PendingManagerApproval);
        request.Status = RequestStatus.Rejected;
        request.DecidedAt = decidedAt;

        request.Actions.Add(new RequestAction
        {
            ActionType = RequestActionType.Rejected,
            FromStatus = RequestStatus.PendingManagerApproval,
            ToStatus = RequestStatus.Rejected,
            Comment = reason,
            ActorUserId = manager.Id,
            CreatedAt = decidedAt,
        });

        Notify(db, request, item.ResponsibleUserId, "RequestRejected",
            $"The renewal request for {item.Name} was rejected by the manager.", decidedAt);
    }

    // stops the seeder if the steps are in the wrong order, e.g. a finance review on a request that doesn't need one
    private static void CheckStatus(RenewalRequest request, RequestStatus expected)
    {
        if (request.Status != expected)
            throw new InvalidOperationException($"The request is {request.Status}, but this step needs {expected}.");
    }

    private static void Notify(AppDbContext db, RenewalRequest request, int recipientUserId, string type, string message, DateTime createdAt)
    {
        db.Notifications.Add(new Notification
        {
            Type = type,
            Message = message,
            Request = request,
            RecipientUserId = recipientUserId,
            CreatedAt = createdAt,
        });
    }
}