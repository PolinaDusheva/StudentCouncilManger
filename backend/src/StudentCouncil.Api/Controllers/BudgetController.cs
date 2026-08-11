using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Budget;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/budget")]
[Authorize]
public sealed class BudgetController : ControllerBase
{
    private readonly ISender _sender;

    public BudgetController(ISender sender) => _sender = sender;

    /// <summary>Total spend for a year (everyone can view — functional 9.2).</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<BudgetSummaryDto>> GetSummary(
        [FromQuery] GetBudgetSummaryQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Paginated, chronological expense list (everyone can view).</summary>
    [HttpGet("expenses")]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> GetExpenses(
        [FromQuery] GetExpensesQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Adds an expense (Year is derived from the spend date).</summary>
    [HttpPost("expenses")]
    [Authorize(Policy = "CanManageBudget")]
    public async Task<ActionResult<ExpenseDto>> Create(
        CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/budget/expenses/{expense.Id}", expense);
    }

    /// <summary>Edits an expense (recomputes the derived year).</summary>
    [HttpPut("expenses/{id:guid}")]
    [Authorize(Policy = "CanManageBudget")]
    public async Task<ActionResult<ExpenseDto>> Update(
        Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(UpdateExpenseCommand.From(id, request), cancellationToken));

    /// <summary>Deletes an expense.</summary>
    [HttpDelete("expenses/{id:guid}")]
    [Authorize(Policy = "CanManageBudget")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteExpenseCommand(id), cancellationToken);
        return NoContent();
    }
}
