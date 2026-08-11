using FluentAssertions;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Auth;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Auth;

public class RefreshHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();

    private RefreshHandler CreateSut() => new(_identity, _jwt, _refreshTokens);

    [Fact]
    public async Task Rotates_and_returns_new_pair()
    {
        var memberId = Guid.NewGuid();
        var account = new MemberAccount(memberId, "ivan@ue-varna.bg", "Ivan", SystemRole.Member,
            Guid.NewGuid(), DepartmentCode.PR, MemberStatus.Active, false, "stamp");

        _refreshTokens.RotateAsync("old", Arg.Any<CancellationToken>())
            .Returns(new RotatedToken("new-refresh", Now, memberId));
        _identity.FindByIdAsync(memberId, Arg.Any<CancellationToken>()).Returns(account);
        _jwt.CreateAccessToken(Arg.Any<TokenUser>()).Returns(new AccessToken("new-access", Now, 7200));

        var result = await CreateSut().Handle(new RefreshCommand("old"), default);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public async Task Deactivated_member_is_rejected_and_all_tokens_revoked()
    {
        var memberId = Guid.NewGuid();
        _refreshTokens.RotateAsync("old", Arg.Any<CancellationToken>())
            .Returns(new RotatedToken("new-refresh", Now, memberId));
        _identity.FindByIdAsync(memberId, Arg.Any<CancellationToken>()).Returns((MemberAccount?)null);

        var act = () => CreateSut().Handle(new RefreshCommand("old"), default);

        (await act.Should().ThrowAsync<UnauthorizedException>()).Which.Code.Should().Be("account_inactive");
        await _refreshTokens.Received().RevokeAllForMemberAsync(memberId, Arg.Any<CancellationToken>());
    }
}
