using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Tasks;
using StudentCouncil.Application.Features.Tasks.Comments;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ISender _sender;

    public TasksController(ISender sender) => _sender = sender;

    /// <summary>Paginated, filterable task list (shaped by the caller's visibility).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskListItemDto>>> GetTasks(
        [FromQuery] GetTasksQuery query, CancellationToken cancellationToken)
        => Ok(await _sender.Send(query, cancellationToken));

    /// <summary>Tasks the caller is assigned to.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<TaskListItemDto>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyTasksQuery(), cancellationToken));

    /// <summary>Kanban board of the visible tasks, grouped by status.</summary>
    [HttpGet("board")]
    public async Task<ActionResult<TaskBoardDto>> GetBoard(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetTaskBoardQuery(), cancellationToken));

    /// <summary>Full task details (404 when not visible to the caller).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetTaskQuery(id), cancellationToken));

    /// <summary>Creates a task (handler enforces the scope rule on top of the policy).</summary>
    [HttpPost]
    [Authorize(Policy = "CanCreateDeptTask")]
    public async Task<ActionResult<TaskDetailDto>> Create(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/tasks/{task.Id}", task);
    }

    /// <summary>Updates a task's title, description, priority, due date and assignees.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanCreateDeptTask")]
    public async Task<ActionResult<TaskDetailDto>> Update(
        Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(UpdateTaskCommand.From(id, request), cancellationToken));

    /// <summary>Changes a task's status (assignee self-service steps or leadership transitions).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TaskDetailDto>> ChangeStatus(
        Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ChangeTaskStatusCommand(id, request.Status), cancellationToken));

    /// <summary>Cancels a task (leadership for the scope/department).</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelTaskCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Physically deletes a task (OrgPresident only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "OrgPresidentOnly")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Chronological list of a task's comments.</summary>
    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<TaskCommentDto>>> GetComments(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetTaskCommentsQuery(id), cancellationToken));

    /// <summary>Adds a comment to a task.</summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<TaskCommentDto>> AddComment(
        Guid id, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await _sender.Send(new AddTaskCommentCommand(id, request.Text), cancellationToken);
        return Created($"/api/v1/tasks/{id}/comments", comment);
    }
}
