using FluentAssertions;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Auth;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Auth;

public class LoginHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();

    private LoginHandler CreateSut() => new(_identity, _jwt, _refreshTokens);

    private static MemberAccount Account(MemberStatus status = MemberStatus.Active, bool mustChange = false) =>
        new(Guid.NewGuid(), "ivan@ue-varna.bg", "Ivan", SystemRole.Member,
            Guid.NewGuid(), DepartmentCode.PR, status, mustChange, "stamp");

    [Fact]
    public async Task Returns_tokens_on_success()
    {
        var account = Account(mustChange: true);
        _identity.FindByEmailAsync(account.Email, Arg.Any<CancellationToken>()).Returns(account);
        _identity.CheckPasswordAsync(account.Id, "pw", true, Arg.Any<CancellationToken>())
            .Returns(new PasswordSignInOutcome(true, false));
        _jwt.CreateAccessToken(Arg.Any<TokenUser>()).Returns(new AccessToken("access", Now, 7200));
        _refreshTokens.IssueAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResult("refresh", Now));

        var result = await CreateSut().Handle(new LoginCommand(account.Email, "pw"), default);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh");
        result.ExpiresInSeconds.Should().Be(7200);
        result.MustChangePassword.Should().BeTrue();
        result.User.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task Unknown_email_is_invalid_credentials()
    {
        _identity.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((MemberAccount?)null);

        var act = () => CreateSut().Handle(new LoginCommand("nobody@ue-varna.bg", "pw"), default);

        (await act.Should().ThrowAsync<UnauthorizedException>()).Which.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Inactive_account_is_rejected_before_password_check()
    {
        var account = Account(MemberStatus.Inactive);
        _identity.FindByEmailAsync(account.Email, Arg.Any<CancellationToken>()).Returns(account);

        var act = () => CreateSut().Handle(new LoginCommand(account.Email, "pw"), default);

        (await act.Should().ThrowAsync<UnauthorizedException>()).Which.Code.Should().Be("account_inactive");
        await _identity.DidNotReceive().CheckPasswordAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Locked_out_returns_423()
    {
        var account = Account();
        _identity.FindByEmailAsync(account.Email, Arg.Any<CancellationToken>()).Returns(account);
        _identity.CheckPasswordAsync(account.Id, "pw", true, Arg.Any<CancellationToken>())
            .Returns(new PasswordSignInOutcome(false, true));

        var act = () => CreateSut().Handle(new LoginCommand(account.Email, "pw"), default);

        (await act.Should().ThrowAsync<LockedException>()).Which.StatusCode.Should().Be(423);
    }

    [Fact]
    public async Task Wrong_password_is_invalid_credentials()
    {
        var account = Account();
        _identity.FindByEmailAsync(account.Email, Arg.Any<CancellationToken>()).Returns(account);
        _identity.CheckPasswordAsync(account.Id, "pw", true, Arg.Any<CancellationToken>())
            .Returns(new PasswordSignInOutcome(false, false));

        var act = () => CreateSut().Handle(new LoginCommand(account.Email, "pw"), default);

        (await act.Should().ThrowAsync<UnauthorizedException>()).Which.Code.Should().Be("invalid_credentials");
    }
}
