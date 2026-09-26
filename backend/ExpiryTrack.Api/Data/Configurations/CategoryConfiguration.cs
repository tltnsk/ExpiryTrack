using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
        {
            t.HasCheckConstraint("CK_Categories_WarningPeriodDays", "WarningPeriodDays BETWEEN 1 AND 365");
            t.HasCheckConstraint("CK_Categories_FinanceReviewThreshold", "FinanceReviewThreshold IS NULL OR FinanceReviewThreshold >= 0");
        });

        builder.Property(c => c.Name).HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(255);
        builder.Property(c => c.FinanceReviewThreshold).HasPrecision(12, 2);
    }
}
