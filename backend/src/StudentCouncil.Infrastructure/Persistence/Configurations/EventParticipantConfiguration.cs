using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.HasKey(p => new { p.CalendarEventId, p.MemberId });

        builder.HasOne(p => p.CalendarEvent)
            .WithMany(e => e.Participants)
            .HasForeignKey(p => p.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.MemberId);
    }
}
