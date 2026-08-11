using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Tasks;
using StudentCouncil.Application.Features.Tasks.Documents;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks/{taskId:guid}/documents")]
[Authorize]
public sealed class TaskDocumentsController : ControllerBase
{
    private const long MaxDocumentBytes = 25L * 1024 * 1024;

    private readonly ISender _sender;

    public TaskDocumentsController(ISender sender) => _sender = sender;

    /// <summary>Document metadata for a task (no internal storage keys).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDocumentDto>>> List(Guid taskId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetTaskDocumentsQuery(taskId), cancellationToken));

    /// <summary>Uploads a document (multipart, field "file"; validated by type + magic bytes + size).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDocumentBytes)]
    public async Task<ActionResult<TaskDocumentDto>> Upload(
        Guid taskId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BadRequestException("A file is required.", "unsupported_file_type");
        }

        var upload = await file.ToUploadAsync(cancellationToken);
        var document = await _sender.Send(new UploadTaskDocumentCommand(taskId, upload), cancellationToken);
        return Created($"/api/v1/tasks/{taskId}/documents/{document.Id}", document);
    }

    /// <summary>Streams a document as an attachment.</summary>
    [HttpGet("{docId:guid}/download")]
    public async Task<IActionResult> Download(Guid taskId, Guid docId, CancellationToken cancellationToken)
    {
        var file = await _sender.Send(new DownloadTaskDocumentQuery(taskId, docId), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Deletes a document (uploader or Org leadership).</summary>
    [HttpDelete("{docId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId, Guid docId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTaskDocumentCommand(taskId, docId), cancellationToken);
        return NoContent();
    }
}
