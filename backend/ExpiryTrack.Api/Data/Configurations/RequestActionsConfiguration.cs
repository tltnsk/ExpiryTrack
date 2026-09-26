using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class RequestActionConfiguration : IEntityTypeConfiguration<RequestAction>
{
    public void Configure(EntityTypeBuilder<RequestAction> builder)
    {
        builder.ToTable("RequestActions", t =>
        {
            t.HasCheckConstraint("CK_RequestActions_ActionType",
                "ActionType IN ('Submitted','FinanceReviewed','ClarificationRequested','ClarificationProvided','Approved','Rejected','Completed','ClosedByExpiry')");
            t.HasCheckConstraint("CK_RequestActions_FromStatus", "FromStatus IN ('PendingFinancialReview','PendingManagerApproval','ClarificationRequired','Approved','Rejected','Completed')");
            t.HasCheckConstraint("CK_RequestActions_ToStatus", "ToStatus IN ('PendingFinancialReview','PendingManagerApproval','ClarificationRequired','Approved','Rejected','Completed')");
            t.HasCheckConstraint("CK_RequestActions_FinancialRecommendation", "FinancialRecommendation IN ('Approve','Reject')");
        });

        builder.Property(a => a.ActionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.FromStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.ToStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.FinancialRecommendation).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.Comment).HasColumnType("text");
    }
}
