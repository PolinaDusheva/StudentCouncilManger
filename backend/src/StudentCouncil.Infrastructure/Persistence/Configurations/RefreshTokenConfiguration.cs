using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);

        builder.Ignore(t => t.IsActive);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.MemberId);
    }
}
