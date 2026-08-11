using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.HasKey(ta => new { ta.TaskItemId, ta.MemberId });

        builder.HasOne(ta => ta.TaskItem)
            .WithMany(t => t.Assignees)
            .HasForeignKey(ta => ta.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ta => ta.MemberId);

        // Match the parent's soft-delete filter so children of deleted tasks stay hidden.
        builder.HasQueryFilter(ta => !ta.TaskItem.IsDeleted);
    }
}
