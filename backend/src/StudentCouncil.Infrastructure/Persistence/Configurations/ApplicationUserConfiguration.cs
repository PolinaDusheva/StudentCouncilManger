using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Infrastructure.Identity;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(120);
        builder.Property(u => u.PhotoUrl).HasMaxLength(512);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(30);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.DepartmentId);
        builder.HasIndex(u => u.Role);
    }
}
