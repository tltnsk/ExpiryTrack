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

    private static ClarificationAnswerRequest EmployeeData(string answer)
    {
        return new ClarificationAnswerRequest()
        {
            Answer = answer,
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
        var managerA = new User { FirstName = "Test", LastName = "Manager A", RoleId = 3 };
        var managerB = new User { FirstName = "Test", LastName = "Manager B", RoleId = 3 };
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

    // AnswerClarificationAsync tests
    [Fact]
    public async Task AnswerClarification_Works()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.ClarificationRequired);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.AnswerClarificationAsync(request.Id, item.ResponsibleUserId, EmployeeData("We need this item for next year."));

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.PendingManagerApproval, result.Request.Status);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.ClarificationProvided, action.ActionType);
        Assert.Equal(RequestStatus.ClarificationRequired, action.FromStatus);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(item.Department.ManagerId, notification.RecipientUserId);
    }

    // request not found and different employee
    [Fact]
    public async Task AnswerClarification_OtherEmployee_NotFound()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var manager2 = new User { FirstName = "Test", LastName = "Manager 2", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);
        var item2 = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 22, manager: manager2);

        var request = TestData.AddRequest(db, item2, RequestStatus.ClarificationRequired);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.AnswerClarificationAsync(request.Id, item.ResponsibleUserId, EmployeeData(" "));

        Assert.True(result.NotFound);
        Assert.Empty(db.RequestActions);
    }

    // wrong status
    [Fact]
    public async Task AnswerClarification_AlreadyProcessed()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.Approved);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.AnswerClarificationAsync(request.Id, item.ResponsibleUserId, EmployeeData(" "));

        Assert.Contains("already been processed", result.Error);
        Assert.Empty(db.RequestActions);
    }

    // no answer provided
    [Fact]
    public async Task AnswerClarification_NoAnswerProvided()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.ClarificationRequired);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.AnswerClarificationAsync(request.Id, item.ResponsibleUserId, EmployeeData(""));

        Assert.Contains("answer", result.Error);
        Assert.Empty(db.RequestActions);
    }

    // Unit tests for CompleteAsync

    // valid case
    [Fact]
    public async Task CompleteAsync_Valid()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.Approved);
        request.ProposedCost = 1000;
        item.EscalatedAt = DateTime.UtcNow;

        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.CompleteAsync(request.Id, item.ResponsibleUserId);

        Assert.Null(result.Error);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatus.Completed, result.Request.Status);

        var action = Assert.Single(db.RequestActions);
        Assert.Equal(RequestActionType.Completed, action.ActionType);
        Assert.Equal(RequestStatus.Approved, action.FromStatus);

        var notification = Assert.Single(db.Notifications);
        Assert.Equal(item.Department.ManagerId, notification.RecipientUserId);

        Assert.Equal(2, db.LifecyclePeriods.Count());

        Assert.Equal(2, item.CurrentPeriod!.PeriodNumber);

        Assert.Equal(1000, item.CurrentPeriod.Cost);

        Assert.Equal(LifecycleState.Active, item.LifecycleState);

        Assert.Equal(item.CurrentPeriod.ExpirationDate, request.ProposedExpirationDate);

        Assert.Null(item.EscalatedAt);

        var history = Assert.Single(db.ItemStateHistory);
        Assert.Equal(LifecycleState.ExpiringSoon, history.FromState);
        Assert.Equal(LifecycleState.Active, history.ToState);

        Assert.NotNull(result.Request.CompletedAt);
    }

    // when renewal is not approved -> error
    [Fact]
    public async Task CompleteAsync_NotApproved()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.Rejected);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.CompleteAsync(request.Id, item.ResponsibleUserId);

        Assert.Contains("not been approved", result.Error);
        Assert.Empty(db.RequestActions);
        Assert.Equal(LifecycleState.ExpiringSoon, item.LifecycleState);
        Assert.Single(db.LifecyclePeriods);
    }

    [Fact]
    public async Task CompleteAsync_DatePassed()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);

        var request = TestData.AddRequest(db, item, RequestStatus.Approved);
        request.ProposedExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        db.SaveChanges();
        var service = new RenewalService(db);

        var result = await service.CompleteAsync(request.Id, item.ResponsibleUserId);

        Assert.Contains("later than today", result.Error);
        Assert.Empty(db.RequestActions);
        Assert.Equal(LifecycleState.ExpiringSoon, item.LifecycleState);
        Assert.Equal(1, db.LifecyclePeriods.Count());
    }

    [Fact]
    public async Task CompleteAsync_AnotherEmployee()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var manager2 = new User { FirstName = "Test", LastName = "Manager 2", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 21, manager: manager);
        var item2 = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 22, manager: manager2);

        var request = TestData.AddRequest(db, item2, RequestStatus.ClarificationRequired);
        db.SaveChanges();
        var service = new RenewalService(db);
        var result = await service.CompleteAsync(request.Id, item.ResponsibleUserId);

        Assert.True(result.NotFound);
        Assert.Empty(db.RequestActions);
    }
}