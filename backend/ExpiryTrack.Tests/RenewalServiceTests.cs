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

    private static ManagerDecisionRequest ManagerData(string comment)
    {
        return new ManagerDecisionRequest()
        {
            Comment = comment,
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

    [Fact]
    public async Task ManagerReview_Approved()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.ApproveAsync(request.Id, manager.Id, ManagerData("Approved"));

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.Approved, result.Request.Status);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.Approved, action.ActionType);
        Assert.Equal(RequestStatus.PendingManagerApproval, action.FromStatus);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(item.ResponsibleUserId, notification.RecipientUserId);
    }

    // wrong status
    [Fact]
    public async Task ManagerReview_WrongStatus()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingFinancialReview);
        request.NeedsFinancialReview = true;
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.ApproveAsync(request.Id, manager.Id, ManagerData(" "));

        Assert.Contains("already been processed", result.Error);
        Assert.Empty(db.RequestActions);
    }
    
    // not this department - not found
    [Fact]
    public async Task ManagerReview_WrongDepartment()
    {
        using var db = TestData.CreateDb();
        var managerA = new User {FirstName = "Test", LastName = "Manager A", RoleId = 3};
        var managerB = new User {FirstName = "Test", LastName = "Manager B", RoleId = 3};
        var itemA = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15, manager: managerA);
        var itemB = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 15, manager: managerB);
        
        var request = TestData.AddRequest(db, itemA, RequestStatus.PendingManagerApproval);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.ApproveAsync(request.Id, managerB.Id, ManagerData(" "));

        Assert.True(result.NotFound);
        Assert.Empty(db.RequestActions);
    }
    
    // reject without a reason should show an error
    [Fact]
    public async Task ManagerReview_RejectWithoutAReason()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.RejectAsync(request.Id, manager.Id, ManagerData(""));
        
        Assert.Contains("Reason", result.Error);
        Assert.Empty(db.RequestActions);
    }
    
    // reject with a reason 
    [Fact]
    public async Task ManagerReview_RejectWithAReason()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.RejectAsync(request.Id, manager.Id, ManagerData("Not needed"));
        
        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.Rejected, result.Request.Status);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.Rejected, action.ActionType);
        Assert.Equal(RequestStatus.PendingManagerApproval, action.FromStatus);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(item.ResponsibleUserId, notification.RecipientUserId);
    }
    
    // clarification with question works
    [Fact]
    public async Task ManagerReview_ClarificationWithQuestion()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.ClarificationAsync(request.Id, manager.Id, ManagerData("Provide more details."));
        
        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.ClarificationRequired, result.Request.Status);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.ClarificationRequested, action.ActionType);
        Assert.Equal(RequestStatus.PendingManagerApproval, action.FromStatus);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(item.ResponsibleUserId, notification.RecipientUserId);
    }
    
    // clarification without question does not work
    [Fact]
    public async Task ManagerReview_ClarificationWithoutQuestion()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.ClarificationAsync(request.Id, manager.Id, ManagerData(""));
        
        Assert.Contains("question", result.Error);
        Assert.Empty(db.RequestActions);
    }
    
    // clarification requested for a second time should show an error
    [Fact]
    public async Task ManagerReview_SecondClarification()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 40, manager: manager);
        var service = new RenewalService(db);

        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        
        db.RequestActions.Add(new RequestAction
        {
            RequestId = request.Id,
            ActionType = RequestActionType.ClarificationRequested,
            ToStatus = RequestStatus.ClarificationRequired,
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();

        var result = await service.ClarificationAsync(request.Id, manager.Id, ManagerData("Again?"));
    
        Assert.NotNull(result.Error);
        Assert.Single(db.RequestActions);
    }
}