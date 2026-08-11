using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Members;

public sealed record GetMyProfileQuery : IRequest<MemberDto>;

public sealed class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, MemberDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMemberDirectory _members;

    public GetMyProfileHandler(ICurrentUser currentUser, IMemberDirectory members)
    {
        _currentUser = currentUser;
        _members = members;
    }

    public async Task<MemberDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Member", userId);

        return view.ToDto();
    }
}

/// <summary>Self-service profile edit — only the phone number (spec 5.6).</summary>
public sealed record UpdateMyProfileCommand(string? PhoneNumber) : IRequest<MemberDto>;

public sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .Matches(@"^\+?[0-9\s\-]{6,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("The phone number is not in a valid format.");
    }
}

public sealed class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, MemberDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IMemberDirectory _members;

    public UpdateMyProfileHandler(ICurrentUser currentUser, IIdentityService identity, IMemberDirectory members)
    {
        _currentUser = currentUser;
        _identity = identity;
        _members = members;
    }

    public async Task<MemberDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var result = await _identity.UpdatePhoneAsync(userId, phone, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? "The profile could not be updated.");
        }

        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Member", userId);

        return view.ToDto();
    }
}
