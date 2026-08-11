using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Location).HasMaxLength(300);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Recurrence).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Events)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StartUtc);
        builder.HasIndex(e => e.DepartmentId);
    }
}
