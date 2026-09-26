using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {

        builder.ToTable("Notifications", t =>
        {
            // notification can be linked to either an item or a request
            t.HasCheckConstraint("CK_Notifications_Link", "ItemId IS NULL OR RequestId IS NULL");
        });

        builder.Property(n => n.Type).HasMaxLength(30);
        builder.Property(n => n.Message).HasMaxLength(500);
    }
}
