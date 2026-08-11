using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Token).IsRequired().HasMaxLength(512);
        builder.Property(d => d.Platform).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(d => d.Token).IsUnique();
        builder.HasIndex(d => d.MemberId);
    }
}
