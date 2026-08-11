using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Members;

public sealed record DeactivateMemberCommand(Guid Id) : IRequest;

public sealed class DeactivateMemberHandler : IRequestHandler<DeactivateMemberCommand>
{
    private readonly IMemberDirectory _members;
    private readonly IIdentityService _identity;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditRecorder _audit;
    private readonly IAppDbContext _db;

    public DeactivateMemberHandler(
        IMemberDirectory members,
        IIdentityService identity,
        IRefreshTokenService refreshTokens,
        ICurrentUser currentUser,
        IAuditRecorder audit,
        IAppDbContext db)
    {
        _members = members;
        _identity = identity;
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(DeactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var target = await _members.Members.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Member", request.Id);

        if (_currentUser.Id == target.Id)
        {
            throw new ConflictException("You cannot deactivate your own account.");
        }

        // Never lock the organisation out of member management.
        if (target.Role == SystemRole.OrgPresident && target.Status == MemberStatus.Active)
        {
            var activePresidents = await _members.Members
                .CountAsync(m => m.Role == SystemRole.OrgPresident && m.Status == MemberStatus.Active, cancellationToken);

            if (activePresidents <= 1)
            {
                throw new ConflictException("The last active organisation president cannot be deactivated.");
            }
        }

        var result = await _identity.SetStatusAsync(request.Id, MemberStatus.Inactive, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? "The member could not be deactivated.");
        }

        // Revoke refresh tokens; the security stamp was already rotated by the status change.
        await _refreshTokens.RevokeAllForMemberAsync(request.Id, cancellationToken);

        _audit.Record(AuditActions.MemberDeactivated, AuditEntities.Member, request.Id, new { memberId = request.Id });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
