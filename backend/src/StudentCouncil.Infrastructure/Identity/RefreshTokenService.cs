using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Infrastructure.Options;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>
/// Opaque, rotating refresh tokens. Only SHA-256 hashes are persisted; presenting
/// an already-rotated token revokes every active token for that member.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly IDateTime _clock;
    private readonly JwtOptions _options;

    public RefreshTokenService(AppDbContext db, IDateTime clock, IOptions<JwtOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<RefreshTokenResult> IssueAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var raw = GenerateRawToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            TokenHash = Hash(raw),
            CreatedAtUtc = _clock.UtcNow,
            ExpiresAtUtc = _clock.UtcNow.AddDays(_options.RefreshTokenDays)
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(raw, entity.ExpiresAtUtc);
    }

    public async Task<RotatedToken> RotateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // Reuse of an already-revoked token => assume theft, revoke the whole chain.
        if (existing.RevokedAtUtc is not null)
        {
            await RevokeAllForMemberAsync(existing.MemberId, cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used.");
        }

        if (_clock.UtcNow >= existing.ExpiresAtUtc)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var raw = GenerateRawToken();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = existing.MemberId,
            TokenHash = Hash(raw),
            CreatedAtUtc = _clock.UtcNow,
            ExpiresAtUtc = _clock.UtcNow.AddDays(_options.RefreshTokenDays)
        };

        existing.RevokedAtUtc = _clock.UtcNow;
        existing.ReplacedByTokenHash = replacement.TokenHash;
        _db.RefreshTokens.Add(replacement);
        await _db.SaveChangesAsync(cancellationToken);

        return new RotatedToken(raw, replacement.ExpiresAtUtc, existing.MemberId);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null)
        {
            return; // idempotent
        }

        existing.RevokedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.MemberId == memberId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAtUtc = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawToken()
    {
        // 256-bit opaque token.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
