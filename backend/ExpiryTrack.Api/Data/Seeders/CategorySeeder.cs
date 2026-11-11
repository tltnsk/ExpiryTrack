using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Seeders;

public static class CategorySeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        db.Categories.AddRange(
            new Category
            {
                Name = "Software Licence",
                Description = "Software licences and subscriptions",
                WarningPeriodDays = 30
            },
            new Category
            {
                Name = "Contract",
                Description = "Service and supplier contracts",
                WarningPeriodDays = 60,
                RequiresFinancialReview = true,
                FinanceReviewThreshold = 5000
            },
            new Category
            {
                Name = "Insurance",
                Description = "Insurance policies",
                WarningPeriodDays = 45,
                RequiresFinancialReview = true,
            },
            new Category
            {
                Name = "Lease",
                Description = "Office and equipment leases",
                WarningPeriodDays = 90,
                RequiresFinancialReview = true,
            },
            new Category
            {
                Name = "Certificate",
                Description = "SSL and domain certificates",
                WarningPeriodDays = 30
            });
        await db.SaveChangesAsync();
    }
}