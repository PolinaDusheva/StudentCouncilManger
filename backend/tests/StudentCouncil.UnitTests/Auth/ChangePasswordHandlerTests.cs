using FluentAssertions;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Auth;
using ValidationException = StudentCouncil.Application.Common.Exceptions.ValidationException;

namespace StudentCouncil.UnitTests.Auth;

public class ChangePasswordHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();

    private readonly Guid _userId = Guid.NewGuid();

    private ChangePasswordHandler CreateSut()
    {
        _currentUser.Id.Returns(_userId);
        return new ChangePasswordHandler(_currentUser, _identity, _refreshTokens);
    }

    [Fact]
    public async Task Success_revokes_all_refresh_tokens()
    {
        _identity.ChangePasswordAsync(_userId, "old", "NewPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        await CreateSut().Handle(new ChangePasswordCommand("old", "NewPass1"), default);

        await _refreshTokens.Received().RevokeAllForMemberAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Wrong_current_password_throws_validation_and_keeps_sessions()
    {
        _identity.ChangePasswordAsync(_userId, "wrong", "NewPass1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure("incorrect", "PasswordMismatch"));

        var act = () => CreateSut().Handle(new ChangePasswordCommand("wrong", "NewPass1"), default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("currentPassword");
        await _refreshTokens.DidNotReceive().RevokeAllForMemberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
