using FluentAssertions;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Security;

public class EventAccessTests
{
    private static readonly Guid Pr = Guid.NewGuid();
    private static readonly Guid Sports = Guid.NewGuid();
    private static readonly Guid Organizer = Guid.NewGuid();

    private sealed class FakeUser : ICurrentUser
    {
        public Guid? Id { get; init; }
        public string? Email => null;
        public SystemRole? Role { get; init; }
        public Guid? DepartmentId { get; init; }
        public bool IsAuthenticated => Id is not null;
    }

    private static CalendarEvent Event(Guid organizerId, Guid? departmentId) =>
        new() { Id = Guid.NewGuid(), OrganizerId = organizerId, DepartmentId = departmentId };

    [Fact]
    public void Owner_can_edit_but_a_secretary_owner_cannot_delete()
    {
        var secretary = new FakeUser { Id = Organizer, Role = SystemRole.DeptSecretary, DepartmentId = Pr };
        var ownEvent = Event(Organizer, Pr);

        EventAccess.CanEdit(ownEvent, secretary).Should().BeTrue();
        EventAccess.CanDelete(ownEvent, secretary).Should().BeFalse();
    }

    [Theory]
    [InlineData(SystemRole.OrgPresident)]
    [InlineData(SystemRole.OrgVicePresident)]
    public void Org_leadership_can_edit_and_delete_any_event(SystemRole role)
    {
        var user = new FakeUser { Id = Guid.NewGuid(), Role = role };
        var foreign = Event(Organizer, Sports);

        EventAccess.CanEdit(foreign, user).Should().BeTrue();
        EventAccess.CanDelete(foreign, user).Should().BeTrue();
    }

    [Theory]
    [InlineData(SystemRole.DeptPresident)]
    [InlineData(SystemRole.DeptVicePresident)]
    public void Dept_leadership_manages_only_their_own_department(SystemRole role)
    {
        var lead = new FakeUser { Id = Guid.NewGuid(), Role = role, DepartmentId = Pr };
        var ownDept = Event(Organizer, Pr);
        var otherDept = Event(Organizer, Sports);

        EventAccess.CanEdit(ownDept, lead).Should().BeTrue();
        EventAccess.CanDelete(ownDept, lead).Should().BeTrue();
        EventAccess.CanEdit(otherDept, lead).Should().BeFalse();
        EventAccess.CanDelete(otherDept, lead).Should().BeFalse();
    }

    [Fact]
    public void Dept_leadership_cannot_manage_an_event_without_a_department()
    {
        var lead = new FakeUser { Id = Guid.NewGuid(), Role = SystemRole.DeptPresident, DepartmentId = Pr };
        var noDept = Event(Organizer, departmentId: null);

        EventAccess.CanEdit(noDept, lead).Should().BeFalse();
        EventAccess.CanDelete(noDept, lead).Should().BeFalse();
    }

    [Fact]
    public void Member_can_neither_edit_nor_delete_even_if_they_organise_it()
    {
        // A plain Member never reaches these helpers (the CanManageEvents policy blocks the endpoint),
        // but the resource rule is defensive: organiser-or-leadership only, and Member is neither.
        var member = new FakeUser { Id = Organizer, Role = SystemRole.Member, DepartmentId = Pr };
        var ownEvent = Event(Organizer, Pr);

        // Organiser can edit regardless of role (mirrors functional 8.2 owner-edit)...
        EventAccess.CanEdit(ownEvent, member).Should().BeTrue();
        // ...but never delete.
        EventAccess.CanDelete(ownEvent, member).Should().BeFalse();
    }

    [Fact]
    public void EnsureCanDelete_throws_Forbidden_for_a_non_owner_secretary()
    {
        var secretary = new FakeUser { Id = Guid.NewGuid(), Role = SystemRole.OrgSecretary };
        var someoneElses = Event(Organizer, Pr);

        var act = () => EventAccess.EnsureCanDelete(someoneElses, secretary);

        act.Should().Throw<ForbiddenException>();
    }
}
