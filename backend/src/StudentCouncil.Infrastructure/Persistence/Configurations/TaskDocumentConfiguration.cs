using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class TaskDocumentConfiguration : IEntityTypeConfiguration<TaskDocument>
{
    public void Configure(EntityTypeBuilder<TaskDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(260);
        builder.Property(d => d.StorageKey).IsRequired().HasMaxLength(1024);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(150);

        builder.HasOne(d => d.TaskItem)
            .WithMany(t => t.Documents)
            .HasForeignKey(d => d.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.TaskItemId);

        // Match the parent's soft-delete filter so children of deleted tasks stay hidden.
        builder.HasQueryFilter(d => !d.TaskItem.IsDeleted);
    }
}
