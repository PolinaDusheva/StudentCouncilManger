using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Text).IsRequired().HasMaxLength(2000);

        builder.HasOne(c => c.TaskItem)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.TaskItemId);
        builder.HasIndex(c => c.AuthorId);

        // Match the parent's soft-delete filter so children of deleted tasks stay hidden.
        builder.HasQueryFilter(c => !c.TaskItem.IsDeleted);
    }
}
