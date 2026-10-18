using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Tests;

public class RenewalServiceTests
{
    // create a SubmitRenewalRequest object
    private static SubmitRenewalRequest SubmitData(decimal? cost = null)
    {
        return new SubmitRenewalRequest
        {
            ProposedExpirationDate = TestData.Today.AddYears(1),
            ProposedCost = cost,
            Justification = "Item still needed.",
        };
    }

    private static FinanceReviewRequest FinanceData(FinancialRecommendation recommendation)
    {
        return new FinanceReviewRequest
        {
            Recommendation = recommendation,
            Comment = "Good to go",
        };
    }

    [Fact]
    public async Task CategoryWithoutReview()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 500));

        Assert.Null(result.Error);
        Assert.False(result.NotFound);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingManagerApproval, result.Request.Status);
        Assert.False(result.Request.NeedsFinancialReview);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.Submitted, action.ActionType);
        Assert.Null(action.FromStatus);
        Assert.Equal(item.ResponsibleUserId, action.ActorUserId);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(manager.Id, notification.RecipientUserId);
    }

    [Fact]
    public async Task FinancialReviewRequired_NoThreshold()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", Role = new Role { Id = 4, Name = "FinanceOfficer" } };
        db.Users.Add(finance);
        db.SaveChanges();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager, requiresFinancialReview: true);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 500));

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingFinancialReview, result.Request.Status);
        Assert.True(result.Request.NeedsFinancialReview);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(finance.Id, notification.RecipientUserId);
    }

    [Fact]
    public async Task FinancialReview_Threshold()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", Role = new Role { Id = 4, Name = "FinanceOfficer" } };
        db.Users.Add(finance);
        db.SaveChanges();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager, requiresFinancialReview: true, threshold: 5000);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 5000));

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingFinancialReview, result.Request.Status);
        Assert.True(result.Request.NeedsFinancialReview);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(finance.Id, notification.RecipientUserId);
    }

    [Fact]
    public async Task CostBelowThreshold()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", Role = new Role { Id = 4, Name = "FinanceOfficer" } };
        db.Users.Add(finance);
        db.SaveChanges();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager, requiresFinancialReview: true, threshold: 5000);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 300));

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingManagerApproval, result.Request.Status);
        Assert.False(result.Request.NeedsFinancialReview);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(manager.Id, notification.RecipientUserId);
    }

    [Fact]
    public async Task NoCost()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", Role = new Role { Id = 4, Name = "FinanceOfficer" } };
        db.Users.Add(finance);
        db.SaveChanges();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager, requiresFinancialReview: true, threshold: 5000);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData());

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingManagerApproval, result.Request.Status);
        Assert.False(result.Request.NeedsFinancialReview);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(manager.Id, notification.RecipientUserId);
    }

    [Fact]
    public async Task SubmitActiveItem()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 40, manager: manager, requiresFinancialReview: true, threshold: 5000);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 300));

        Assert.NotNull(result.Error);
        Assert.Empty(db.RenewalRequests);
    }

    [Fact]
    public async Task SubmitOpenRequest()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 40, manager: manager, requiresFinancialReview: true, threshold: 5000);
        TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 300));

        Assert.NotNull(result.Error);
        Assert.Single(db.RenewalRequests);
    }

    [Fact]
    public async Task SubmitNoManager()
    {
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 20);
        var service = new RenewalService(db);

        var result = await service.SubmitAsync(item.Id, item.ResponsibleUserId, SubmitData(cost: 300));

        Assert.NotNull(result.Error);
        Assert.Empty(db.RenewalRequests);
    }

    [Fact]
    public async Task FinanceReview_Approved()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingFinancialReview);
        request.NeedsFinancialReview = true;
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.FinanceReviewAsync(request.Id, 99, FinanceData(FinancialRecommendation.Approve));

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingManagerApproval, result.Request.Status);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.FinanceReviewed, action.ActionType);
        Assert.Equal(RequestStatus.PendingFinancialReview, action.FromStatus);
        Assert.Equal(FinancialRecommendation.Approve, action.FinancialRecommendation);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(manager.Id, notification.RecipientUserId);
    }

    [Fact]
    public async Task FinanceReview_WrongStatus()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        request.NeedsFinancialReview = true;
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.FinanceReviewAsync(request.Id, 99, FinanceData(FinancialRecommendation.Approve));

        Assert.Contains("already been processed", result.Error);
        Assert.Empty(db.RequestActions);
    }

    [Fact]
    public async Task FinanceReview_RequestNotFound()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var service = new RenewalService(db);

        var result = await service.FinanceReviewAsync(999, 99, FinanceData(FinancialRecommendation.Approve));

        Assert.True(result.NotFound);
    }
}