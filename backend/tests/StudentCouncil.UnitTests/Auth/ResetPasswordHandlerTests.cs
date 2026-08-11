using FluentAssertions;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Auth;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Auth;

public class ResetPasswordHandlerTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();

    private ResetPasswordHandler CreateSut() => new(_identity, _refreshTokens);

    [Fact]
    public async Task Invalid_token_returns_400_invalid_reset_token()
    {
        _identity.ResetPasswordAsync("a@b.bg", "bad", "NewPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure("invalid", "invalid_reset_token"));

        var act = () => CreateSut().Handle(new ResetPasswordCommand("a@b.bg", "bad", "NewPass1"), default);

        var ex = (await act.Should().ThrowAsync<BadRequestException>()).Which;
        ex.StatusCode.Should().Be(400);
        ex.Code.Should().Be("invalid_reset_token");
    }

    [Fact]
    public async Task Success_revokes_refresh_tokens_for_the_member()
    {
        var account = new MemberAccount(Guid.NewGuid(), "a@b.bg", "A", SystemRole.Member,
            Guid.NewGuid(), DepartmentCode.PR, MemberStatus.Active, false, "stamp");
        _identity.ResetPasswordAsync("a@b.bg", "tok", "NewPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _identity.FindByEmailAsync("a@b.bg", Arg.Any<CancellationToken>()).Returns(account);

        await CreateSut().Handle(new ResetPasswordCommand("a@b.bg", "tok", "NewPass1"), default);

        await _refreshTokens.Received().RevokeAllForMemberAsync(account.Id, Arg.Any<CancellationToken>());
    }
}
