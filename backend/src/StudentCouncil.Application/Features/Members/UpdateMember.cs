using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Members;

/// <summary>Body of <c>PUT /members/{id}</c> (email is immutable, so it is not included).</summary>
public sealed record UpdateMemberRequest(
    string FullName,
    string? PhoneNumber,
    SystemRole Role,
    Guid? DepartmentId,
    DateOnly JoinedOn);

public sealed record UpdateMemberCommand(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    SystemRole Role,
    Guid? DepartmentId,
    DateOnly JoinedOn) : IRequest<MemberDto>
{
    public static UpdateMemberCommand From(Guid id, UpdateMemberRequest request) =>
        new(id, request.FullName, request.PhoneNumber, request.Role, request.DepartmentId, request.JoinedOn);
}

public sealed class UpdateMemberValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberValidator(IAppDbContext db)
    {
        RuleFor(x => x.FullName).NotEmpty().Length(2, 120);

        RuleFor(x => x.Role).IsInEnum();

        RuleFor(x => x.DepartmentId)
            .Null().When(x => RoleRules.IsOrgRole(x.Role))
            .WithMessage("Organisation roles must not be assigned to a department.");

        RuleFor(x => x.DepartmentId)
            .NotNull().When(x => !RoleRules.IsOrgRole(x.Role))
            .WithMessage("This role requires a department.");

        RuleFor(x => x.DepartmentId!)
            .MustAsync(async (id, ct) => await db.Departments.AnyAsync(d => d.Id == id, ct))
            .When(x => x.DepartmentId.HasValue)
            .WithMessage("The specified department does not exist.");
    }
}

public sealed class UpdateMemberHandler : IRequestHandler<UpdateMemberCommand, MemberDto>
{
    private readonly IIdentityService _identity;
    private readonly IMemberDirectory _members;
    private readonly IAuditRecorder _audit;
    private readonly IAppDbContext _db;

    public UpdateMemberHandler(
        IIdentityService identity, IMemberDirectory members, IAuditRecorder audit, IAppDbContext db)
    {
        _identity = identity;
        _members = members;
        _audit = audit;
        _db = db;
    }

    public async Task<MemberDto> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        // Snapshot the prior state for the diff before the identity update mutates it (decision #4).
        var before = await _members.Members.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        var result = await _identity.UpdateMemberAsync(
            request.Id,
            new MemberUpdate(request.FullName, request.PhoneNumber, request.Role, request.DepartmentId, request.JoinedOn),
            cancellationToken);

        if (!result.Succeeded)
        {
            throw result.Code == "not_found"
                ? new NotFoundException("Member", request.Id)
                : new ConflictException(result.Error ?? "The member could not be updated.");
        }

        if (before is not null)
        {
            // Spec 14 calls out role changes specifically; a role/department change is a RoleChanged event.
            var roleOrDepartmentChanged = before.Role != request.Role || before.DepartmentId != request.DepartmentId;
            var changes = new AuditDetails()
                .Change("fullName", before.FullName, request.FullName)
                .Change("phoneNumber", before.PhoneNumber, request.PhoneNumber)
                .Change("role", before.Role, request.Role)
                .Change("departmentId", before.DepartmentId, request.DepartmentId)
                .Change("joinedOn", before.JoinedOn, request.JoinedOn);

            _audit.Record(
                roleOrDepartmentChanged ? AuditActions.RoleChanged : AuditActions.MemberUpdated,
                AuditEntities.Member, request.Id, changes.Values);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Member", request.Id);

        return view.ToDto();
    }
}
