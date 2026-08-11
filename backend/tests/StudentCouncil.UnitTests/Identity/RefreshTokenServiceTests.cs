using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Options;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Identity;

public class RefreshTokenServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (RefreshTokenService Sut, AppDbContext Db) CreateSut()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"refresh-{Guid.NewGuid()}")
            .Options);

        var clock = Substitute.For<IDateTime>();
        clock.UtcNow.Returns(FixedNow);

        var options = Options.Create(new JwtOptions
        {
            Issuer = "i",
            Audience = "a",
            SigningKey = "unit-tests-signing-key-at-least-32-bytes!!",
            RefreshTokenDays = 30
        });

        return (new RefreshTokenService(db, clock, options), db);
    }

    [Fact]
    public async Task IssueAsync_persists_only_the_hash_and_returns_the_raw_token()
    {
        var (sut, db) = CreateSut();
        var memberId = Guid.NewGuid();

        var result = await sut.IssueAsync(memberId);

        result.RawToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().Be(FixedNow.AddDays(30));

        var stored = await db.RefreshTokens.SingleAsync();
        stored.MemberId.Should().Be(memberId);
        stored.TokenHash.Should().NotBe(result.RawToken);
        stored.TokenHash.Should().HaveLength(64); // SHA-256 hex
        stored.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RotateAsync_revokes_the_old_token_and_links_the_replacement()
    {
        var (sut, db) = CreateSut();
        var memberId = Guid.NewGuid();
        var first = await sut.IssueAsync(memberId);

        var second = await sut.RotateAsync(first.RawToken);

        second.RawToken.Should().NotBe(first.RawToken);

        var tokens = await db.RefreshTokens.OrderBy(t => t.CreatedAtUtc).ToListAsync();
        tokens.Should().HaveCount(2);

        var old = tokens.Single(t => t.RevokedAtUtc != null);
        old.ReplacedByTokenHash.Should().NotBeNull();

        var active = tokens.Single(t => t.RevokedAtUtc == null);
        active.ExpiresAtUtc.Should().Be(FixedNow.AddDays(30));
    }

    [Fact]
    public async Task RotateAsync_reusing_a_rotated_token_is_rejected_and_revokes_the_chain()
    {
        var (sut, db) = CreateSut();
        var memberId = Guid.NewGuid();
        var first = await sut.IssueAsync(memberId);
        await sut.RotateAsync(first.RawToken); // first is now revoked

        var act = () => sut.RotateAsync(first.RawToken);

        await act.Should().ThrowAsync<UnauthorizedException>();

        // Reuse detected => every token for the member is revoked.
        var anyActive = await db.RefreshTokens.AnyAsync(t => t.RevokedAtUtc == null);
        anyActive.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAsync_is_idempotent_for_unknown_tokens()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.RevokeAsync("not-a-real-token");

        await act.Should().NotThrowAsync();
    }
}
