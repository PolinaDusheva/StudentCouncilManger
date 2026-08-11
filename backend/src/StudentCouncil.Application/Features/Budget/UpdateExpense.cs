using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Budget;

/// <summary>Body of <c>PUT /budget/expenses/{id}</c>; the route supplies the id.</summary>
public sealed record UpdateExpenseRequest(
    string Description,
    decimal AmountEur,
    DateOnly SpentOn);

public sealed record UpdateExpenseCommand(
    Guid Id,
    string Description,
    decimal AmountEur,
    DateOnly SpentOn) : IRequest<ExpenseDto>, IExpenseInput
{
    public static UpdateExpenseCommand From(Guid id, UpdateExpenseRequest request) =>
        new(id, request.Description, request.AmountEur, request.SpentOn);
}

public sealed class UpdateExpenseValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseValidator(IDateTime clock) => ExpenseRules.Apply(this, clock);
}

public sealed class UpdateExpenseHandler : IRequestHandler<UpdateExpenseCommand, ExpenseDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly IAuditRecorder _audit;

    public UpdateExpenseHandler(IAppDbContext db, IMemberDirectory members, IAuditRecorder audit)
    {
        _db = db;
        _members = members;
        _audit = audit;
    }

    public async Task<ExpenseDto> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Expense", request.Id);

        var description = request.Description.Trim();

        // Diff the old values against the new ones before mutating (decision #4).
        var changes = new AuditDetails()
            .Change("amountEur", expense.AmountEur, request.AmountEur)
            .Change("spentOn", expense.SpentOn, request.SpentOn)
            .Change("description", expense.Description, description);

        expense.Description = description;
        expense.AmountEur = request.AmountEur;
        expense.SpentOn = request.SpentOn;
        // Keep the derived year in step with the (possibly changed) spend date.
        expense.Year = request.SpentOn.Year;

        _audit.Record(AuditActions.ExpenseUpdated, AuditEntities.Expense, expense.Id, changes.Values);
        await _db.SaveChangesAsync(cancellationToken);

        return await ExpenseMappings.ToDtoAsync(_members, expense, cancellationToken);
    }
}
