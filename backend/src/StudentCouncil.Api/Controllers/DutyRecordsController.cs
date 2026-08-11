using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Features.Duties;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/duty-records")]
[Authorize]
public sealed class DutyRecordsController : ControllerBase
{
    private readonly ISender _sender;

    public DutyRecordsController(ISender sender) => _sender = sender;

    /// <summary>Duty records: a plain member sees only their own; duty management sees all (with filters).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DutyRecordDto>>> GetRecords(
        [FromQuery] GetDutyRecordsQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Monthly norm table across all active members (under-norm first).</summary>
    [HttpGet("summary")]
    [Authorize(Policy = "CanManageDuties")]
    public async Task<ActionResult<IReadOnlyList<DutySummaryDto>>> GetSummary(
        [FromQuery] GetDutySummaryQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>The caller's own monthly duty standing.</summary>
    [HttpGet("me/summary")]
    public async Task<ActionResult<MyDutySummaryDto>> GetMySummary(
        [FromQuery] GetMyDutySummaryQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Registers a duty (duration + reporting period are computed server-side).</summary>
    [HttpPost]
    [Authorize(Policy = "CanManageDuties")]
    public async Task<ActionResult<DutyRecordDto>> Create(
        CreateDutyRecordCommand command, CancellationToken cancellationToken)
    {
        var record = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/duty-records/{record.Id}", record);
    }

    /// <summary>Edits a duty's shift; duration and period are recomputed.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageDuties")]
    public async Task<ActionResult<DutyRecordDto>> Update(
        Guid id, UpdateDutyRecordRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(UpdateDutyRecordCommand.From(id, request), cancellationToken));

    /// <summary>Deletes a duty record.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageDuties")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteDutyRecordCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Sends a duty reminder to a member, or to everyone under norm, and reports the count.</summary>
    [HttpPost("remind")]
    [Authorize(Policy = "CanManageDuties")]
    public async Task<ActionResult<RemindResult>> Remind(
        [FromBody] RemindDutiesCommand? command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command ?? new RemindDutiesCommand(), cancellationToken));
}
