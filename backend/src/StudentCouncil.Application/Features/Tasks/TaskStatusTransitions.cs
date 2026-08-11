using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>
/// Task status state machine (spec 7.4). Assignees may only push a task forward through the two
/// self-service steps; leadership with edit rights may move it to any other status (including
/// <c>InReview → Completed</c> and <c>* → Cancelled</c>). A no-op or disallowed transition is a 409.
/// </summary>
public static class TaskStatusTransitions
{
    private static readonly IReadOnlyDictionary<TaskStatus, TaskStatus[]> AssigneeMoves =
        new Dictionary<TaskStatus, TaskStatus[]>
        {
            [TaskStatus.New] = [TaskStatus.InProgress],        // "Взимам я"
            [TaskStatus.InProgress] = [TaskStatus.InReview]    // "За проверка"
        };

    public static bool IsAllowed(TaskStatus from, TaskStatus to, bool actorIsLeadership)
    {
        if (from == to)
        {
            return false;
        }

        if (actorIsLeadership)
        {
            return true;
        }

        return AssigneeMoves.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static void EnsureAllowed(TaskStatus from, TaskStatus to, bool actorIsLeadership)
    {
        if (!IsAllowed(from, to, actorIsLeadership))
        {
            throw new ConflictException(
                $"A task cannot move from {from} to {to}.", "invalid_status_transition");
        }
    }
}
