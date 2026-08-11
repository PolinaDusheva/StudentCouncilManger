using System.Text;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Features.Events;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/events")]
[Authorize]
public sealed class EventsController : ControllerBase
{
    private readonly ISender _sender;

    public EventsController(ISender sender) => _sender = sender;

    /// <summary>Calendar entries in a window: real events (recurrences expanded) + task deadlines.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents(
        [FromQuery] GetEventsQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Exports the same windowed view as an <c>.ics</c> calendar file.</summary>
    [HttpGet("export.ics")]
    public async Task<IActionResult> ExportIcs(
        [FromQuery] ExportEventsIcsQuery query, CancellationToken cancellationToken)
    {
        var ics = await _sender.Send(query, cancellationToken);
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar", "studentcouncil.ics");
    }

    /// <summary>Full event details including participants (404 only when it genuinely does not exist).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetEventQuery(id), cancellationToken));

    /// <summary>Creates an event (handler sets the organiser; returns any schedule conflicts as a warning).</summary>
    [HttpPost]
    [Authorize(Policy = "CanManageEvents")]
    public async Task<ActionResult<EventMutationResult>> Create(
        CreateEventCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/events/{result.Event.Id}", result);
    }

    /// <summary>Updates an event (organiser / own-department leadership / Org leadership — EventAccess).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageEvents")]
    public async Task<ActionResult<EventMutationResult>> Update(
        Guid id, UpdateEventRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(UpdateEventCommand.From(id, request), cancellationToken));

    /// <summary>Deletes an event (own-department leadership / Org leadership; secretaries cannot).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageEvents")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteEventCommand(id), cancellationToken);
        return NoContent();
    }
}
