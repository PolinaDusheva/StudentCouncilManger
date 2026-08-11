using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Members;

public sealed record GetMemberQuery(Guid Id) : IRequest<MemberDto>;

public sealed class GetMemberHandler : IRequestHandler<GetMemberQuery, MemberDto>
{
    private readonly IMemberDirectory _members;

    public GetMemberHandler(IMemberDirectory members) => _members = members;

    public async Task<MemberDto> Handle(GetMemberQuery request, CancellationToken cancellationToken)
    {
        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Member", request.Id);

        return view.ToDto();
    }
}
