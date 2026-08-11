using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class DutyRecordConfiguration : IEntityTypeConfiguration<DutyRecord>
{
    public void Configure(EntityTypeBuilder<DutyRecord> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Note).HasMaxLength(1000);

        builder.HasIndex(d => d.MemberId);
        builder.HasIndex(d => new { d.PeriodYear, d.PeriodMonth });
    }
}
