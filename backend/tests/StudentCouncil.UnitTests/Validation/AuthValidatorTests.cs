using FluentAssertions;
using StudentCouncil.Application.Features.Auth;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.UnitTests.Validation;

public class AuthValidatorTests
{
    [Theory]
    [InlineData("", "pw", false)]
    [InlineData("not-an-email", "pw", false)]
    [InlineData("ivan@ue-varna.bg", "", false)]
    [InlineData("ivan@ue-varna.bg", "pw", true)]
    public void Login_validates_email_and_password(string email, string password, bool expected)
    {
        var result = new LoginValidator().Validate(new LoginCommand(email, password));
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("short1A", false)]       // < 8
    [InlineData("alllowercase1", false)] // no uppercase
    [InlineData("NoDigitsHere", false)]  // no digit
    [InlineData("GoodPass1", true)]
    public void ChangePassword_enforces_complexity(string newPassword, bool expected)
    {
        var result = new ChangePasswordValidator().Validate(new ChangePasswordCommand("current", newPassword));
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void ResetPassword_requires_token_and_strong_password()
    {
        new ResetPasswordValidator()
            .Validate(new ResetPasswordCommand("ivan@ue-varna.bg", "", "GoodPass1"))
            .IsValid.Should().BeFalse();

        new ResetPasswordValidator()
            .Validate(new ResetPasswordCommand("ivan@ue-varna.bg", "token", "GoodPass1"))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("+359 88 123 4567", true)]
    [InlineData("nonsense", false)]
    public void UpdateMyProfile_validates_phone(string? phone, bool expected)
    {
        var result = new UpdateMyProfileValidator().Validate(new UpdateMyProfileCommand(phone));
        result.IsValid.Should().Be(expected);
    }
}
