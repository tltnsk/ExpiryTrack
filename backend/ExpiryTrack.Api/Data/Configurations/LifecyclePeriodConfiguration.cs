using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class LifecyclePeriodConfiguration : IEntityTypeConfiguration<LifecyclePeriod>
{
    public void Configure(EntityTypeBuilder<LifecyclePeriod> builder)
    {
        builder.ToTable("LifecyclePeriods", t =>
        {
            t.HasCheckConstraint("CK_LifecyclePeriods_Dates", "ExpirationDate > StartDate");
            t.HasCheckConstraint("CK_LifecyclePeriods_Cost", "Cost IS NULL OR Cost > 0");
        });

        builder.Property(p => p.Cost).HasPrecision(12, 2);

        // period numbers  are unique per item
        builder.HasIndex(p => new { p.ItemId, p.PeriodNumber }).IsUnique();

        // all periods of an item (one to many)
        builder.HasOne(p => p.Item)
            .WithMany(i => i.Periods)
            .HasForeignKey(p => p.ItemId);

        // the renewal request that produced this period (one to one)
        builder.HasOne(p => p.CreatedFromRequest)
            .WithOne(r => r.CreatedPeriod)
            .HasForeignKey<LifecyclePeriod>(p => p.CreatedFromRequestId);
    }
}