using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.Data;
using ExpiryTrack.Api.Models;
using ExpiryTrack.Api.Models.Enums;

namespace ExpiryTrack.Tests;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesDemoData()
    {
        using var db = TestData.CreateEmptyDb();
        
        await DbSeeder.SeedAsync(db, generatedItems: 20);
        
        Assert.Equal(14, db.Users.Count());
        Assert.Equal(4, db.Roles.Count());
        Assert.Equal(5, db.Departments.Count());
        Assert.Equal(5, db.Categories.Count());
        Assert.Equal(35, db.Items.Count());
        Assert.Equal(5, db.RenewalRequests.Count());
        Assert.Equal(9, db.RequestActions.Count());
        Assert.All(db.Departments.ToList(), d => Assert.NotNull(d.ManagerId));
    }
    
    [Fact]
    public async Task SeedAsync_DemoRequests_HaveExpectedStatuses()
    {
        using var db = TestData.CreateEmptyDb();
        
        await DbSeeder.SeedAsync(db, generatedItems: 20);
        List<RenewalRequest> requests = db.RenewalRequests
            .Include(r => r.Period)
            .ThenInclude(p => p.Item)
            .ToList();

        RenewalRequest microsoft = requests.Single(r => r.Period.Item.Name == "Microsoft 365 Business");
        Assert.Equal(RequestStatus.PendingManagerApproval, microsoft.Status);
        
        RenewalRequest officeLease = requests.Single(r => r.Period.Item.Name == "Office lease Sofia HQ");
        Assert.Equal(RequestStatus.PendingFinancialReview, officeLease.Status);
        
        RenewalRequest insurance = requests.Single(r => r.Period.Item.Name == "Company car insurance");
        Assert.Equal(RequestStatus.ClarificationRequired, insurance.Status);
        
        RenewalRequest certificate = requests.Single(r => r.Period.Item.Name == "SSL certificate for company website");
        Assert.Equal(RequestStatus.Approved, certificate.Status);
        
        RenewalRequest cloud = requests.Single(r => r.Period.Item.Name == "Cloud backup service");
        Assert.Equal(RequestStatus.Rejected, cloud.Status);
    }
    
    [Fact]
    public async Task SeedAsync_RunTwice_SeedsOnlyOnce()
    {
        using var db = TestData.CreateEmptyDb();
        
        await DbSeeder.SeedAsync(db, generatedItems: 20);
        await DbSeeder.SeedAsync(db, generatedItems: 20);
        
        Assert.Equal(14, db.Users.Count());
        Assert.Equal(4, db.Roles.Count());
        Assert.Equal(5, db.Departments.Count());
        Assert.Equal(5, db.Categories.Count());
        Assert.Equal(35, db.Items.Count());
        Assert.Equal(5, db.RenewalRequests.Count());
        Assert.Equal(9, db.RequestActions.Count());
        Assert.All(db.Departments.ToList(), d => Assert.NotNull(d.ManagerId));
    }
}