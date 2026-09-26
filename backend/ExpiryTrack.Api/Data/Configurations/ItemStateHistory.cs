using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class ItemStateHistoryConfiguration : IEntityTypeConfiguration<ItemStateHistory>
{
    public void Configure(EntityTypeBuilder<ItemStateHistory> builder)
    {
        builder.ToTable("ItemStateHistory", t =>
        {
            t.HasCheckConstraint("CK_ItemStateHistory_FromState", "FromState IN ('Active','ExpiringSoon','Expired','Cancelled')");
            t.HasCheckConstraint("CK_ItemStateHistory_ToState", "ToState IN ('Active','ExpiringSoon','Expired','Cancelled')");
        });

        builder.Property(h => h.FromState).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToState).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Reason).HasMaxLength(255);
    }
}
