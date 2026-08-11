using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.HasKey(j => j.JobName);
        builder.Property(j => j.JobName).HasMaxLength(100);
    }
}
