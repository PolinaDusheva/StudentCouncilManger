using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Features.Departments;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Authorize]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ISender _sender;

    public DepartmentsController(ISender sender) => _sender = sender;

    /// <summary>All departments with member counts and leadership summaries.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetDepartments(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetDepartmentsQuery(), cancellationToken));

    /// <summary>Department details plus its members.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepartmentDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetDepartmentQuery(id), cancellationToken));
}
