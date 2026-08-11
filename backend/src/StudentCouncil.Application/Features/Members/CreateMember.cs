using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Email;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Members;

public sealed record CreateMemberCommand(
    string FullName,
    string Email,
    string? PhoneNumber,
    SystemRole Role,
    Guid? DepartmentId,
    DateOnly JoinedOn) : IRequest<MemberDto>;

public sealed class CreateMemberValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberValidator(IMemberDirectory members, IAppDbContext db)
    {
        RuleFor(x => x.FullName).NotEmpty().Length(2, 120);

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress()
            .MustAsync(async (email, ct) =>
                !await members.Members.AnyAsync(m => m.Email.ToLower() == email.ToLower(), ct))
            .WithMessage("A member with this email already exists.");

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

public sealed class CreateMemberHandler : IRequestHandler<CreateMemberCommand, MemberDto>
{
    private readonly IIdentityService _identity;
    private readonly IMemberDirectory _members;
    private readonly IEmailSender _email;
    private readonly IAuditRecorder _audit;
    private readonly IAppDbContext _db;

    public CreateMemberHandler(
        IIdentityService identity, IMemberDirectory members, IEmailSender email,
        IAuditRecorder audit, IAppDbContext db)
    {
        _identity = identity;
        _members = members;
        _email = email;
        _audit = audit;
        _db = db;
    }

    public async Task<MemberDto> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.CreateMemberAsync(
            new NewMember(request.FullName, request.Email, request.PhoneNumber,
                request.Role, request.DepartmentId, request.JoinedOn),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            throw new ConflictException(result.Error ?? "The member could not be created.");
        }

        var created = result.Value;

        // Audit the creation as soon as it is durable, before the (best-effort) welcome email.
        _audit.Record(AuditActions.MemberCreated, AuditEntities.Member, created.Id,
            new { request.Email, request.Role, request.DepartmentId });
        await _db.SaveChangesAsync(cancellationToken);

        var message = EmailTemplates.WelcomeWithTempPassword(request.FullName, request.Email, created.TemporaryPassword);
        await _email.SendAsync(request.Email, message.Subject, message.HtmlBody, cancellationToken);

        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == created.Id, cancellationToken)
            ?? throw new NotFoundException("Member", created.Id);

        return view.ToDto();
    }
}
