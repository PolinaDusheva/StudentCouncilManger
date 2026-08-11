using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Members;

public sealed record ReactivateMemberCommand(Guid Id) : IRequest;

public sealed class ReactivateMemberHandler : IRequestHandler<ReactivateMemberCommand>
{
    private readonly IMemberDirectory _members;
    private readonly IIdentityService _identity;
    private readonly IAuditRecorder _audit;
    private readonly IAppDbContext _db;

    public ReactivateMemberHandler(
        IMemberDirectory members, IIdentityService identity, IAuditRecorder audit, IAppDbContext db)
    {
        _members = members;
        _identity = identity;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(ReactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var exists = await _members.Members.AnyAsync(m => m.Id == request.Id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Member", request.Id);
        }

        var result = await _identity.SetStatusAsync(request.Id, MemberStatus.Active, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? "The member could not be reactivated.");
        }

        _audit.Record(AuditActions.MemberReactivated, AuditEntities.Member, request.Id, new { memberId = request.Id });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
