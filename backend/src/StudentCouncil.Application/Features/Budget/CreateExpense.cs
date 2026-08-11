using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Budget;

public sealed record CreateExpenseCommand(
    string Description,
    decimal AmountEur,
    DateOnly SpentOn) : IRequest<ExpenseDto>, IExpenseInput;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator(IDateTime clock) => ExpenseRules.Apply(this, clock);
}

public sealed class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, ExpenseDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditRecorder _audit;

    public CreateExpenseHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, IAuditRecorder audit)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<ExpenseDto> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var addedById = _currentUser.Id ?? throw new UnauthorizedException();

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            AmountEur = request.AmountEur,
            SpentOn = request.SpentOn,
            // Year is derived from the spend date for cheap filtering (decision #10).
            Year = request.SpentOn.Year,
            AddedById = addedById
        };

        _db.Expenses.Add(expense);
        _audit.Record(AuditActions.ExpenseAdded, AuditEntities.Expense, expense.Id,
            new { expense.AmountEur, expense.SpentOn, expense.Description });
        await _db.SaveChangesAsync(cancellationToken);

        return await ExpenseMappings.ToDtoAsync(_members, expense, cancellationToken);
    }
}

/// <summary>Common shape of the create/update expense inputs so the rules can be written once.</summary>
internal interface IExpenseInput
{
    string Description { get; }
    decimal AmountEur { get; }
    DateOnly SpentOn { get; }
}

/// <summary>Expense validation rules shared by create and update (spec 11, plan 4.4.1).</summary>
internal static class ExpenseRules
{
    public static void Apply<T>(AbstractValidator<T> validator, IDateTime clock)
        where T : IExpenseInput
    {
        validator.RuleFor(x => x.Description).NotEmpty().Length(1, 300);

        validator.RuleFor(x => x.AmountEur)
            .GreaterThan(0).WithMessage("The amount must be greater than zero.");

        validator.RuleFor(x => x.AmountEur)
            .Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("The amount must have at most two decimal places.");

        validator.RuleFor(x => x.SpentOn)
            .Must(spentOn => spentOn <= DateOnly.FromDateTime(clock.UtcNow))
            .WithMessage("The spend date cannot be in the future.");
    }
}
