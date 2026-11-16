using ExpiryTrack.Api.Controllers;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpiryTrack.Tests;

public class CategoriesControllerTests
{
    [Fact]
    public async Task Create_NewName_Created()
    {
        using var db = TestData.CreateDb();
        var controller = new CategoriesController(db);

        var result = await controller.Create(new CategoryRequest() { Name = "Contracts" });
        
        var created = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<CategoryResponse>(created.Value);
        Assert.Equal("Contracts", response.Name);
        Assert.Single(db.Categories);
    }
    
    [Fact]
    public async Task Create_DuplicateName_Conflict()
    {
        using var db = TestData.CreateDb();
        db.Categories.Add(new Category { Name = "Contracts" });
        db.SaveChanges();
        var controller = new CategoriesController(db);

        var result = await controller.Create(new CategoryRequest() { Name = "Contracts" });
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("A category with that name already exists.", conflict.Value);
        Assert.Single(db.Categories);
    }
    // Crate_NoReview_ThresholdCleared
    [Fact]
    public async Task Create_NoReview_ThresholdCleared()
    {
        using var db = TestData.CreateDb();
        var controller = new CategoriesController(db);
        
        await controller.Create(new CategoryRequest
        {
            Name = "Contract", 
            WarningPeriodDays = 30, RequiresFinancialReview = false,
            FinanceReviewThreshold = 5000, IsActive = true
        });

        var category = Assert.Single(db.Categories);
        Assert.Null(category.FinanceReviewThreshold);
    }
    
    [Fact]
    public async Task Create_WithReview_ThresholdKept()
    {
        using var db = TestData.CreateDb();
        var controller = new CategoriesController(db);
        
        await controller.Create(new CategoryRequest
        {
            Name = "Contract", 
            WarningPeriodDays = 30, RequiresFinancialReview = true,
            FinanceReviewThreshold = 5000, IsActive = true
        });

        var category = Assert.Single(db.Categories);
        Assert.NotNull(category.FinanceReviewThreshold);
        Assert.Equal(5000m, category.FinanceReviewThreshold);
    }
    
    [Fact]
    public async Task Update_MissingCategory_NotFound()
    {
        using var db = TestData.CreateDb();
        var controller = new CategoriesController(db);
        
        var result = await controller.Update(999, new CategoryRequest() { Name = "Contract"});
        
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(notFound);
    }
    
    [Fact]
    public async Task Update_NameOfOtherCategory_Conflict()
    {
        using var db = TestData.CreateDb();
        db.Categories.Add(new Category { Name = "Contract" });
        Category licenses = new Category { Name = "License" };
        db.Categories.Add(licenses);
        db.SaveChanges();
        var controller = new CategoriesController(db);

        var result = await controller.Update(licenses.Id, new CategoryRequest() { Name = "Contract"});
        
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("A category with that name already exists.", conflict.Value);
        Assert.Equal("License", licenses.Name);
    }
}