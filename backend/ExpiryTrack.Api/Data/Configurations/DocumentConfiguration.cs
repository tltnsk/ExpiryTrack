using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", t =>
        {
            // A document belongs to exactly one item or one request
            t.HasCheckConstraint("CK_Documents_Owner", "(ItemId IS NULL) <> (RequestId IS NULL)");
            // Max 10 MB
            t.HasCheckConstraint("CK_Documents_Size", "SizeBytes > 0 AND SizeBytes <= 10485760");
        });
        builder.Property(d => d.OriginalFileName).HasMaxLength(255);
        builder.Property(d => d.StoredFileName).HasMaxLength(255);

        builder.HasIndex(d => d.StoredFileName).IsUnique();
    }
}
