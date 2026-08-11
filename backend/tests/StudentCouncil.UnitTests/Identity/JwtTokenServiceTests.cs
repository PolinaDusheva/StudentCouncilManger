using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Options;

namespace StudentCouncil.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static JwtTokenService CreateSut()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "https://issuer.test",
            Audience = "test-audience",
            AccessTokenMinutes = 120,
            RefreshTokenDays = 30,
            SigningKey = "unit-tests-signing-key-at-least-32-bytes!!"
        });

        var clock = Substitute.For<IDateTime>();
        clock.UtcNow.Returns(FixedNow);

        return new JwtTokenService(options, clock);
    }

    [Fact]
    public void CreateAccessToken_writes_expected_claims()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var user = new TokenUser(userId, "ivan@ue-varna.bg", "Ivan Ivanov",
            SystemRole.DeptPresident, deptId, DepartmentCode.PR, "stamp-123", MustChangePassword: false);

        var result = sut.CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Subject.Should().Be(userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "DeptPresident");
        jwt.Claims.Should().Contain(c => c.Type == "dept" && c.Value == "PR");
        jwt.Claims.Should().Contain(c => c.Type == "deptId" && c.Value == deptId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "stamp" && c.Value == "stamp-123");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "ivan@ue-varna.bg");
        jwt.Claims.Should().NotContain(c => c.Type == "must_change_password");
    }

    [Fact]
    public void CreateAccessToken_includes_must_change_password_claim_when_flagged()
    {
        var sut = CreateSut();
        var user = new TokenUser(Guid.NewGuid(), "new@ue-varna.bg", "New Member",
            SystemRole.Member, Guid.NewGuid(), DepartmentCode.Social, "stamp", MustChangePassword: true);

        var result = sut.CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == "must_change_password" && c.Value == "true");
    }

    [Fact]
    public void CreateAccessToken_sets_issuer_audience_and_expiry()
    {
        var sut = CreateSut();
        var user = new TokenUser(Guid.NewGuid(), "org@ue-varna.bg", "Org President",
            SystemRole.OrgPresident, DepartmentId: null, Department: null, "stamp", MustChangePassword: false);

        var result = sut.CreateAccessToken(user);

        result.ExpiresInSeconds.Should().Be(7200);
        result.ExpiresAtUtc.Should().Be(FixedNow.AddMinutes(120));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Issuer.Should().Be("https://issuer.test");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.ValidTo.Should().BeCloseTo(FixedNow.AddMinutes(120), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateAccessToken_omits_department_claims_for_org_roles()
    {
        var sut = CreateSut();
        var user = new TokenUser(Guid.NewGuid(), "org@ue-varna.bg", "Org President",
            SystemRole.OrgPresident, DepartmentId: null, Department: null, "stamp", MustChangePassword: false);

        var result = sut.CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().NotContain(c => c.Type == "dept");
        jwt.Claims.Should().NotContain(c => c.Type == "deptId");
    }
}
