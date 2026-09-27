using System.ComponentModel.DataAnnotations;

namespace ExpiryTrack.Api.DTO;

// what the admin sends to create or update a category
public class CategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(255)]
    public string? Description { get; set; }

    [Range(1, 365)] 
    public int WarningPeriodDays { get; set; } = 30;

    public bool RequiresFinancialReview { get; set; }
    
    [Range(0, 9999999.99)]
    public decimal? FinanceReviewThreshold { get; set; }

    public bool IsActive { get; set; } = true;

}

// what the server sends back about a category
public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int WarningPeriodDays { get; set; }
    public bool RequiresFinancialReview { get; set; }
    public decimal? FinanceReviewThreshold { get; set; }
    public bool IsActive { get; set; }
}