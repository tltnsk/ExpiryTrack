using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;
using ExpiryTrack.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class RenewalRequestsControllerTests
{
    [Fact]
    public async Task GetAllRequests_Manager_OnlyOwnDepartment()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var otherItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        TestData.AddRequest(db, otherItem, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, manager.Id);
        var result = await controller.GetAllRequests();
        
        var ok =  Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<RenewalRequestResponse>>(ok.Value);
        var only = Assert.Single(list);
        Assert.Equal(request.Id, only.Id);
    }
    
    [Fact]
    public async Task GetAllRequests_Employee_OnlyTheirOwn()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var otherManager = new User { FirstName = "Test", LastName = "Other Manager", Email = "othermanager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var otherItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: otherManager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        TestData.AddRequest(db, otherItem, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, item.ResponsibleUserId);
        
        var result = await controller.GetAllRequests();
        
        var ok =  Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<RenewalRequestResponse>>(ok.Value);
        var only = Assert.Single(list);
        Assert.Equal(request.Id, only.Id);
    }
    
    [Fact]
    public async Task GetAllRequests_Finance_ThoseWhoNeedFinancialReview()
    {
        using var db = TestData.CreateDb();
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", RoleId = 4 };
        db.Users.Add(finance);
        db.SaveChanges();

        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, requiresFinancialReview: true);
        var otherItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingFinancialReview);
        request.NeedsFinancialReview = true;
        db.SaveChanges();
        TestData.AddRequest(db, otherItem, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, finance.Id);
        
        var result = await controller.GetAllRequests();
        
        var ok =  Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<RenewalRequestResponse>>(ok.Value);
        var only = Assert.Single(list);
        Assert.Equal(request.Id, only.Id);
    }
    
    [Fact]
    public async Task GetAllRequests_Admin_SeesAll()
    {
        using var db = TestData.CreateDb();
        var admin = new User { FirstName = "Test", LastName = "Admin", Email = "admin@test.com", RoleId = 1 };
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var otherManager = new User { FirstName = "Test", LastName = "Other Manager", Email = "othermanager@test.com", RoleId = 3 };
        db.Users.Add(admin);
        db.SaveChanges();
        
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var otherItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: otherManager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        TestData.AddRequest(db, otherItem, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, admin.Id);
        
        var result = await controller.GetAllRequests();
        
        var ok =  Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<RenewalRequestResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetById_ManagerOfDepartment_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, manager.Id);

        var result = await controller.GetById(request.Id);
                
        var ok =  Assert.IsType<OkObjectResult>(result);
        var req = Assert.IsType<RenewalRequestResponse>(ok.Value);
        Assert.Equal(request.Id, req.Id);
    }
    
    [Fact]
    public async Task GetById_ManagerOfOtherDepartment_NotFound()
    {
        using var db = TestData.CreateDb();
        var otherManager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: otherManager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, otherManager.Id);

        var result = await controller.GetById(request.Id);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetForItem_ResponsibleEmployee_AllRequestsOfItem()
    {    
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        TestData.AddRequest(db, item, RequestStatus.Rejected);
        TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);

        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var result = await controller.GetForItem(item.Id);
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<RenewalRequestResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
    }
    
    [Fact]
    public async Task GetForItem_OtherEmployee_NotFound()
    {    
        using var db = TestData.CreateDb();
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var otherItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        TestData.AddRequest(db, item, RequestStatus.Rejected);
  
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, otherItem.ResponsibleUserId);

        var result = await controller.GetForItem(item.Id);
        
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Submit_Valid_Created()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        // the service compares the proposed date with today's date
        var data = new SubmitRenewalRequest
        {
            ProposedExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            Justification = "Still needed"
        };
        var result = await controller.Submit(item.Id, data);
        
        var created = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<RenewalRequestResponse>(created.Value);
        Assert.Equal(RequestStatus.PendingManagerApproval, response.Status);
        Assert.Single(db.RenewalRequests);
    }
    
        [Fact]
    public async Task Submit_ActiveItem_BadRequest()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.Active, daysLeft: 100, manager: manager);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var data = new SubmitRenewalRequest
        {
            ProposedExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            Justification = "Still needed"
        };
        var result = await controller.Submit(item.Id, data);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("can only be renewed", badRequest.Value!.ToString());
        Assert.Empty(db.RenewalRequests);
    }

    [Fact]
    public async Task Submit_NotResponsibleEmployee_NotFound()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var otherItem = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, otherItem.ResponsibleUserId);

        var data = new SubmitRenewalRequest
        {
            ProposedExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1),
            Justification = "Still needed"
        };
        var result = await controller.Submit(item.Id, data);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(db.RenewalRequests);
    }

    [Fact]
    public async Task FinanceReview_Valid_Ok()
    {
        using var db = TestData.CreateDb();
        var finance = new User { FirstName = "Test", LastName = "Finance", Email = "finance@test.com", RoleId = 4 };
        db.Users.Add(finance);
        db.SaveChanges();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager, requiresFinancialReview: true);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingFinancialReview);
        request.NeedsFinancialReview = true;
        db.SaveChanges();
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, finance.Id);

        var data = new FinanceReviewRequest { Recommendation = FinancialRecommendation.Approve, Comment = "Cost is fine" };
        var result = await controller.PostFinanceReview(request.Id, data);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.PendingManagerApproval, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task Approve_ManagerOfDepartment_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, manager.Id);

        var result = await controller.PostApprove(request.Id, new ManagerDecisionRequest { Comment = "Approved" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.Approved, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task Approve_ManagerOfOtherDepartment_NotFound()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var otherManager = new User { FirstName = "Test", LastName = "Other Manager", Email = "othermanager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: otherManager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, otherManager.Id);

        var result = await controller.PostApprove(request.Id, new ManagerDecisionRequest { Comment = "Approved" });

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(RequestStatus.PendingManagerApproval, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task Reject_WithoutReason_BadRequest()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, manager.Id);

        var result = await controller.PostReject(request.Id, new ManagerDecisionRequest { Comment = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Reason is required when rejecting a request.", badRequest.Value);
        Assert.Equal(RequestStatus.PendingManagerApproval, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task Reject_WithReason_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, manager.Id);

        var result = await controller.PostReject(request.Id, new ManagerDecisionRequest { Comment = "Too expensive" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.Rejected, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task RequestClarification_Valid_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.PendingManagerApproval);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, manager.Id);

        var result = await controller.PostClarification(request.Id, new ManagerDecisionRequest { Comment = "Why this price?" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.ClarificationRequired, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task ProvideClarification_Valid_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.ClarificationRequired);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var result = await controller.PostProvideClarification(request.Id, new ClarificationAnswerRequest { Answer = "The price went up" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.PendingManagerApproval, db.RenewalRequests.Find(request.Id)!.Status);
    }

    [Fact]
    public async Task CompleteRenewal_Approved_Ok()
    {
        using var db = TestData.CreateDb();
        var manager = new User { FirstName = "Test", LastName = "Manager", Email = "manager@test.com", RoleId = 3 };
        var item = TestData.AddItem(db, LifecycleState.ExpiringSoon, daysLeft: 20, manager: manager);
        var request = TestData.AddRequest(db, item, RequestStatus.Approved);
        var controller = new RenewalRequestsController(new RenewalService(db), db);
        TestData.SetUser(controller, item.ResponsibleUserId);

        var result = await controller.PostCompleteRenewal(request.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(RequestStatus.Completed, db.RenewalRequests.Find(request.Id)!.Status);
    }
}