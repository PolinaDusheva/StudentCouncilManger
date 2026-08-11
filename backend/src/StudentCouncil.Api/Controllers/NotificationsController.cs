using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Notifications;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>The caller's in-app notifications (newest first; optionally only unread).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> Get(
        [FromQuery] GetNotificationsQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Marks one notification read (404 if it isn't the caller's).</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Marks all of the caller's unread notifications read.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return NoContent();
    }
}
