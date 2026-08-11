using FluentValidation;

namespace StudentCouncil.Application.Common.Validation;

/// <summary>Shared password complexity rule (spec 6.4: ≥ 8 chars, ≥ 1 uppercase, ≥ 1 digit).</summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
}
