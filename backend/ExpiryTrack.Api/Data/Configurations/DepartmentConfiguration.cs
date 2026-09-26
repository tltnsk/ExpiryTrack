using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpiryTrack.Api.Models;

namespace ExpiryTrack.Api.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(100);

        builder.HasIndex(d => d.Name).IsUnique();

        // a department has at most one manager
        // a user manages at most one department
        builder.HasOne(d => d.Manager)
            .WithOne(u => u.ManagedDepartment)
            .HasForeignKey<Department>(d => d.ManagerId);
    }
}
