using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Budget;

/// <summary>Physically deletes an expense (not soft-deletable; a plain remove is a true delete).</summary>
public sealed record DeleteExpenseCommand(Guid Id) : IRequest;

public sealed class DeleteExpenseHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IAppDbContext _db;
    private readonly IAuditRecorder _audit;

    public DeleteExpenseHandler(IAppDbContext db, IAuditRecorder audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Expense", request.Id);

        _audit.Record(AuditActions.ExpenseDeleted, AuditEntities.Expense, expense.Id,
            new { expense.AmountEur, expense.SpentOn });

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
