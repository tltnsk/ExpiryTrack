using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class RenewalRequestConfiguration : IEntityTypeConfiguration<RenewalRequest>
{
    public void Configure(EntityTypeBuilder<RenewalRequest> builder)
    {
        builder.ToTable("RenewalRequests", t =>
        {
            t.HasCheckConstraint("CK_RenewalRequests_Status",
                "Status IN ('PendingFinancialReview','PendingManagerApproval','ClarificationRequired','Approved','Rejected','Completed')");

            t.HasCheckConstraint("CK_RenewalRequests_ProposedCost", "ProposedCost IS NULL OR ProposedCost > 0");
        });

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.ProposedCost).HasPrecision(12, 2);
        builder.Property(r => r.Justification).HasColumnType("text");

        // the period being renewed
        // one to many: a period can have several requests over time
        builder.HasOne(r => r.Period)
            .WithMany(p => p.RenewalRequests)
            .HasForeignKey(r => r.PeriodId);
    }
}
