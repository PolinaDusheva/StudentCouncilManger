using FluentAssertions;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Security;

public class TaskAccessTests
{
    private static readonly Guid Pr = Guid.NewGuid();
    private static readonly Guid Sports = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid Assignee = Guid.NewGuid();
    private static readonly Guid Outsider = Guid.NewGuid();

    private sealed class FakeUser : ICurrentUser
    {
        public Guid? Id { get; init; }
        public string? Email => null;
        public SystemRole? Role { get; init; }
        public Guid? DepartmentId { get; init; }
        public bool IsAuthenticated => Id is not null;
    }

    private static TaskItem Task(TaskScope scope, Guid? departmentId, Guid creator, params Guid[] assignees)
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Scope = scope, DepartmentId = departmentId, CreatedById = creator };
        foreach (var memberId in assignees)
        {
            task.Assignees.Add(new TaskAssignee { TaskItemId = task.Id, MemberId = memberId });
        }

        return task;
    }

    [Fact]
    public void Member_sees_only_tasks_they_created_or_are_assigned_to()
    {
        var member = new FakeUser { Id = Assignee, Role = SystemRole.Member, DepartmentId = Pr };
        var assigned = Task(TaskScope.Departmental, Pr, Creator, Assignee);
        var foreign = Task(TaskScope.Departmental, Pr, Creator, Outsider);

        TaskAccess.CanView(assigned, member).Should().BeTrue();
        TaskAccess.CanView(foreign, member).Should().BeFalse();
    }

    [Fact]
    public void Member_created_task_is_visible_even_without_assignment()
    {
        var member = new FakeUser { Id = Creator, Role = SystemRole.Member, DepartmentId = Pr };
        var own = Task(TaskScope.Departmental, Pr, Creator, Outsider);

        TaskAccess.CanView(own, member).Should().BeTrue();
    }

    [Theory]
    [InlineData(SystemRole.OrgPresident)]
    [InlineData(SystemRole.OrgVicePresident)]
    [InlineData(SystemRole.OrgSecretary)]
    public void Org_roles_see_every_task(SystemRole role)
    {
        var user = new FakeUser { Id = Guid.NewGuid(), Role = role };
        var foreign = Task(TaskScope.Departmental, Sports, Creator, Outsider);

        TaskAccess.CanView(foreign, user).Should().BeTrue();
    }

    [Theory]
    [InlineData(SystemRole.DeptPresident)]
    [InlineData(SystemRole.DeptVicePresident)]
    [InlineData(SystemRole.DeptSecretary)]
    public void Dept_leadership_sees_own_department_tasks_but_not_other_departments(SystemRole role)
    {
        var lead = new FakeUser { Id = Guid.NewGuid(), Role = role, DepartmentId = Pr };
        var ownDept = Task(TaskScope.Departmental, Pr, Creator, Outsider);
        var otherDept = Task(TaskScope.Departmental, Sports, Creator, Outsider);
        var orgTask = Task(TaskScope.Organizational, null, Creator, Outsider);

        TaskAccess.CanView(ownDept, lead).Should().BeTrue();
        TaskAccess.CanView(otherDept, lead).Should().BeFalse();
        // An organisational task they neither created nor are assigned to stays hidden from dept leadership.
        TaskAccess.CanView(orgTask, lead).Should().BeFalse();
    }

    [Fact]
    public void Visible_query_filters_to_the_callers_tasks()
    {
        var member = new FakeUser { Id = Assignee, Role = SystemRole.Member, DepartmentId = Pr };
        var tasks = new[]
        {
            Task(TaskScope.Departmental, Pr, Creator, Assignee),
            Task(TaskScope.Departmental, Pr, Creator, Outsider),
            Task(TaskScope.Organizational, null, Creator, Outsider)
        }.AsQueryable();

        var visible = TaskAccess.Visible(tasks, member).ToList();

        visible.Should().ContainSingle()
            .Which.Assignees.Should().Contain(a => a.MemberId == Assignee);
    }

    [Fact]
    public void Org_leadership_can_edit_any_task_while_dept_leadership_is_scoped()
    {
        var orgVp = new FakeUser { Id = Guid.NewGuid(), Role = SystemRole.OrgVicePresident };
        var deptPresPr = new FakeUser { Id = Guid.NewGuid(), Role = SystemRole.DeptPresident, DepartmentId = Pr };
        var ownDept = Task(TaskScope.Departmental, Pr, Creator, Assignee);
        var otherDept = Task(TaskScope.Departmental, Sports, Creator, Assignee);

        TaskAccess.CanEdit(otherDept, orgVp).Should().BeTrue();
        TaskAccess.CanEdit(ownDept, deptPresPr).Should().BeTrue();
        TaskAccess.CanEdit(otherDept, deptPresPr).Should().BeFalse();
    }

    [Theory]
    [InlineData(SystemRole.DeptSecretary)]
    [InlineData(SystemRole.OrgSecretary)]
    [InlineData(SystemRole.Member)]
    public void Secretaries_and_members_cannot_edit(SystemRole role)
    {
        var user = new FakeUser { Id = Guid.NewGuid(), Role = role, DepartmentId = Pr };
        var task = Task(TaskScope.Departmental, Pr, Creator, Assignee);

        TaskAccess.CanEdit(task, user).Should().BeFalse();
        TaskAccess.CanCancel(task, user).Should().BeFalse();
    }

    [Fact]
    public void EnsureCanView_throws_NotFound_when_hidden()
    {
        var member = new FakeUser { Id = Outsider, Role = SystemRole.Member, DepartmentId = Pr };
        var foreign = Task(TaskScope.Departmental, Pr, Creator, Assignee);

        var act = () => TaskAccess.EnsureCanView(foreign, member);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void EnsureCanEdit_throws_Forbidden_when_visible_but_not_editable()
    {
        // A dept secretary can view their department's task but must not edit it.
        var secretary = new FakeUser { Id = Guid.NewGuid(), Role = SystemRole.DeptSecretary, DepartmentId = Pr };
        var task = Task(TaskScope.Departmental, Pr, Creator, Assignee);

        var act = () => TaskAccess.EnsureCanEdit(task, secretary);

        act.Should().Throw<ForbiddenException>();
    }
}
