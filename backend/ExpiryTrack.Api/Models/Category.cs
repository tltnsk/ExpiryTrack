namespace ExpiryTrack.Api.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public int WarningPeriodDays { get; set; } = 30;

    public bool RequiresFinancialReview { get; set; }

    //if set, finance review is needed only when the proposed cost >= threshold
    public decimal? FinanceReviewThreshold { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}