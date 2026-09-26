using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items", t =>
        {
            t.HasCheckConstraint("CK_Items_LifecycleState", "LifecycleState IN ('Active','ExpiringSoon','Expired','Cancelled')");
        });

        builder.Property(i => i.Name).HasMaxLength(150);
        builder.Property(i => i.Description).HasColumnType("text");
        builder.Property(i => i.Provider).HasMaxLength(150);
        builder.Property(i => i.ReferenceNumber).HasMaxLength(100);
        builder.Property(i => i.LifecycleState).HasConversion<string>().HasMaxLength(20);

        // The period that is valid now 
        builder.HasOne(i => i.CurrentPeriod)
            .WithOne()
            .HasForeignKey<Item>(i => i.CurrentPeriodId);
    }
}
