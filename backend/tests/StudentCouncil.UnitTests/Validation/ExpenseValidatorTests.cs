using FluentAssertions;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Features.Budget;

namespace StudentCouncil.UnitTests.Validation;

public class ExpenseValidatorTests
{
    private static readonly DateTime Now = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private static CreateExpenseValidator Validator()
    {
        var clock = Substitute.For<IDateTime>();
        clock.UtcNow.Returns(Now);
        return new CreateExpenseValidator(clock);
    }

    private static CreateExpenseCommand Expense(decimal amount, DateOnly spentOn, string description = "Stationery") =>
        new(description, amount, spentOn);

    [Fact]
    public void Non_positive_amount_is_rejected()
    {
        Validator().Validate(Expense(0m, Today)).IsValid.Should().BeFalse();
        Validator().Validate(Expense(-5m, Today)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void More_than_two_decimal_places_is_rejected()
    {
        Validator().Validate(Expense(12.345m, Today)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Future_spend_date_is_rejected()
    {
        Validator().Validate(Expense(10m, Today.AddDays(1))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_description_is_rejected()
    {
        Validator().Validate(Expense(10m, Today, description: "")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void A_well_formed_expense_passes()
    {
        Validator().Validate(Expense(12.34m, Today)).IsValid.Should().BeTrue();
        // The spend date may be today.
        Validator().Validate(Expense(99.99m, Today)).IsValid.Should().BeTrue();
    }
}
