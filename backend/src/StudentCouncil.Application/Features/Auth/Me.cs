using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Auth;

/// <summary>Current user: profile + role + department + computed permission flags for the UI.</summary>
public sealed record MeResponse(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    SystemRole Role,
    DepartmentCode? Department,
    string? DepartmentName,
    MemberStatus Status,
    PermissionSet Permissions);

public sealed record GetMeQuery : IRequest<MeResponse>;

public sealed class GetMeHandler : IRequestHandler<GetMeQuery, MeResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMemberDirectory _members;

    public GetMeHandler(ICurrentUser currentUser, IMemberDirectory members)
    {
        _currentUser = currentUser;
        _members = members;
    }

    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Member", userId);

        return new MeResponse(
            view.Id, view.FullName, view.Email, view.PhoneNumber, view.Role,
            view.DepartmentCode, view.DepartmentName, view.Status, MemberPermissions.For(view.Role));
    }
}
