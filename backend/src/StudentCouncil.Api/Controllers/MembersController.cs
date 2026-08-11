using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/members")]
[Authorize]
public sealed class MembersController : ControllerBase
{
    private readonly ISender _sender;

    public MembersController(ISender sender) => _sender = sender;

    /// <summary>Paginated, filterable member list.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MemberSummaryDto>>> GetMembers(
        [FromQuery] GetMembersQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Current user's own profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MemberDto>> GetMyProfile(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyProfileQuery(), cancellationToken));

    /// <summary>Self-service update of the phone number.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<MemberDto>> UpdateMyProfile(
        UpdateMyProfileCommand command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command, cancellationToken));

    /// <summary>Self-service profile photo upload (multipart, field "file"; images only, ≤ 5 MB).</summary>
    [HttpPut("me/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5L * 1024 * 1024)]
    public async Task<ActionResult<MemberDto>> UpdateMyPhoto(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BadRequestException("A file is required.", "unsupported_file_type");
        }

        var upload = await file.ToUploadAsync(cancellationToken);
        return Ok(await _sender.Send(new UpdateMyPhotoCommand(upload), cancellationToken));
    }

    /// <summary>Streams a member's avatar (the public target of <c>photoUrl</c>).</summary>
    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken cancellationToken)
    {
        var photo = await _sender.Send(new GetMemberPhotoQuery(id), cancellationToken);
        return File(photo.Content, photo.ContentType);
    }

    /// <summary>Full profile of a member.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMemberQuery(id), cancellationToken));

    /// <summary>Creates a member account and emails a temporary password.</summary>
    [HttpPost]
    [Authorize(Policy = "CanManageMembers")]
    public async Task<ActionResult<MemberDto>> Create(CreateMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/members/{member.Id}", member);
    }

    /// <summary>Updates a member's role, department and contact details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageMembers")]
    public async Task<ActionResult<MemberDto>> Update(
        Guid id, UpdateMemberRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(UpdateMemberCommand.From(id, request), cancellationToken));

    /// <summary>Deactivates a member (revokes tokens, rotates the security stamp).</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "CanManageMembers")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateMemberCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Reactivates a previously deactivated member.</summary>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = "CanManageMembers")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ReactivateMemberCommand(id), cancellationToken);
        return NoContent();
    }
}
