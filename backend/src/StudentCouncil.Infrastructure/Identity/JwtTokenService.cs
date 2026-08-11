using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Infrastructure.Options;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>Mints HS256 access tokens. Claim names match the spec (short form, no inbound mapping).</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IDateTime _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IDateTime clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken CreateAccessToken(TokenUser user)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new("role", user.Role.ToString()),
            new("stamp", user.SecurityStamp),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Gate flag for the first-login password change; read by the API filter.
        if (user.MustChangePassword)
        {
            claims.Add(new Claim("must_change_password", "true"));
        }

        // Spec: `dept` carries the DepartmentCode; `deptId` (server-side convenience) the Guid.
        if (user.Department is { } dept)
        {
            claims.Add(new Claim("dept", dept.ToString()));
        }

        if (user.DepartmentId is { } deptId)
        {
            claims.Add(new Claim("deptId", deptId.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(jwt, expires, _options.AccessTokenMinutes * 60);
    }
}
