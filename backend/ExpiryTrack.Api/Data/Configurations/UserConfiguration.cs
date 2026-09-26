using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.PasswordHash).HasMaxLength(255);

        builder.HasIndex(u => u.Email).IsUnique();

        //  many users belong to one department 
        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId);
    }
}
