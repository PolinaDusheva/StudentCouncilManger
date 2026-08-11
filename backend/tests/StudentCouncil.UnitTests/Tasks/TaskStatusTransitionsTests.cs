using FluentAssertions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Tasks;
using TaskStatus = StudentCouncil.Domain.Enums.TaskStatus;

namespace StudentCouncil.UnitTests.Tasks;

public class TaskStatusTransitionsTests
{
    [Theory]
    [InlineData(TaskStatus.New, TaskStatus.InProgress)]
    [InlineData(TaskStatus.InProgress, TaskStatus.InReview)]
    public void Assignee_may_take_and_submit_for_review(TaskStatus from, TaskStatus to)
    {
        TaskStatusTransitions.IsAllowed(from, to, actorIsLeadership: false).Should().BeTrue();
    }

    [Theory]
    [InlineData(TaskStatus.New, TaskStatus.InReview)]
    [InlineData(TaskStatus.New, TaskStatus.Completed)]
    [InlineData(TaskStatus.InReview, TaskStatus.Completed)]
    [InlineData(TaskStatus.InProgress, TaskStatus.New)]
    [InlineData(TaskStatus.New, TaskStatus.Cancelled)]
    public void Assignee_may_not_make_other_transitions(TaskStatus from, TaskStatus to)
    {
        TaskStatusTransitions.IsAllowed(from, to, actorIsLeadership: false).Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskStatus.InReview, TaskStatus.Completed)]
    [InlineData(TaskStatus.New, TaskStatus.Cancelled)]
    [InlineData(TaskStatus.InProgress, TaskStatus.Cancelled)]
    [InlineData(TaskStatus.Completed, TaskStatus.InProgress)]
    public void Leadership_may_make_any_distinct_transition(TaskStatus from, TaskStatus to)
    {
        TaskStatusTransitions.IsAllowed(from, to, actorIsLeadership: true).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Same_status_is_never_a_valid_transition(bool isLeadership)
    {
        TaskStatusTransitions.IsAllowed(TaskStatus.InProgress, TaskStatus.InProgress, isLeadership).Should().BeFalse();
    }

    [Fact]
    public void EnsureAllowed_throws_conflict_with_specific_code()
    {
        var act = () => TaskStatusTransitions.EnsureAllowed(TaskStatus.New, TaskStatus.Completed, actorIsLeadership: false);

        var ex = act.Should().Throw<ConflictException>().Which;
        ex.StatusCode.Should().Be(409);
        ex.Code.Should().Be("invalid_status_transition");
    }
}
