using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(150);
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Scope).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(t => t.Department)
            .WithMany(d => d.Tasks)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.DepartmentId);
        builder.HasIndex(t => t.DueAtUtc);

        // Preserve history: hide soft-deleted tasks globally.
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
