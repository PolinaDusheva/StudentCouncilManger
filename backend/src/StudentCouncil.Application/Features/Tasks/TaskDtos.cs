using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>Compact task row for lists and the Kanban board.</summary>
public sealed record TaskListItemDto(
    Guid Id,
    string Title,
    TaskPriority Priority,
    TaskStatus Status,
    DateTime? DueAtUtc,
    TaskScope Scope,
    DepartmentCode? Department,
    int AssigneeCount,
    int CommentCount,
    int DocumentCount,
    bool IsOverdue);

/// <summary>Full task view: metadata + people + attachments + comments.</summary>
public sealed record TaskDetailDto(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    TaskStatus Status,
    DateTime? DueAtUtc,
    TaskScope Scope,
    DepartmentCode? Department,
    MemberSummaryDto? CreatedBy,
    DateTime CreatedAtUtc,
    IReadOnlyList<MemberSummaryDto> Assignees,
    IReadOnlyList<TaskDocumentDto> Documents,
    IReadOnlyList<TaskCommentDto> Comments);

/// <summary>Kanban columns (Cancelled is intentionally not a column — functional 6.6).</summary>
public sealed record TaskBoardDto(TaskBoardColumns Columns);

public sealed record TaskBoardColumns(
    IReadOnlyList<TaskListItemDto> New,
    IReadOnlyList<TaskListItemDto> InProgress,
    IReadOnlyList<TaskListItemDto> InReview,
    IReadOnlyList<TaskListItemDto> Completed);

public sealed record TaskCommentDto(
    Guid Id,
    MemberSummaryDto? Author,
    string Text,
    DateTime CreatedAtUtc);

/// <summary>Document metadata (never exposes the internal <c>StorageKey</c>).</summary>
public sealed record TaskDocumentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    MemberSummaryDto? UploadedBy,
    DateTime UploadedAtUtc);
