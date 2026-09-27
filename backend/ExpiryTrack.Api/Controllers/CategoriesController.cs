using ExpiryTrack.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpiryTrack.Api.DTO;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Controllers;


[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }
    
    // GET /api/categories
    // no restriction, since any logged-in user can see the categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        var responses = new List<CategoryResponse>();

        foreach (var category in categories)
        {
            var response = ToResponse(category);
            responses.Add(response);
        }
        
        return Ok(responses);
    }
    
    // GET /api/categories/{id} 
    // get only one category 
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) 
            return NotFound();
        
        CategoryResponse respCategory = ToResponse(category);
        return Ok(respCategory);
    }
    
    // POST /api/categories
    // only an administrator can create an item category (FR-ADM-04)
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> Create(CategoryRequest request)
    {
        var name = request.Name.Trim();
        
        if (await _db.Categories.AnyAsync(c => c.Name == name))
            return Conflict("A category with that name already exists.");

        var category = new Category();
        CopyFromRequest(request, category);
        
        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();
        
        return Created($"/api/categories/{category.Id}", ToResponse(category));
    }
    
    // PUT /api/categories/{id}
    // update a category - only administrator can do this
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
            return NotFound();
        
        var name =  category.Name.Trim();
        
        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != id))
            return Conflict("A category with that name already exists.");
        
        CopyFromRequest(request, category);
        
        await _db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }
  
    // helper method
    // takes data received from frontend and copies it into a database entity
    private static void CopyFromRequest(CategoryRequest request, Category category)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.WarningPeriodDays = request.WarningPeriodDays;
        category.RequiresFinancialReview = request.RequiresFinancialReview;
        
        // a threshold only makes sense when financial review is required
        category.FinanceReviewThreshold = request.RequiresFinancialReview ? request.FinanceReviewThreshold : null;
        category.IsActive = request.IsActive;
    }

    // maps a category in the database to category response 
    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            WarningPeriodDays = category.WarningPeriodDays,
            RequiresFinancialReview = category.RequiresFinancialReview,
            FinanceReviewThreshold = category.FinanceReviewThreshold,
            IsActive = category.IsActive
        };
    }
}