using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Files;

namespace StudentCouncil.Application.Features.Members;

/// <summary>Self-service profile photo upload (functional 5.6). Images only; overwrites the previous avatar.</summary>
public sealed record UpdateMyPhotoCommand(FileUpload File) : IRequest<MemberDto>;

public sealed class UpdateMyPhotoHandler : IRequestHandler<UpdateMyPhotoCommand, MemberDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IMemberDirectory _members;
    private readonly IFileStorage _storage;
    private readonly StorageLimits _limits;

    public UpdateMyPhotoHandler(
        ICurrentUser currentUser,
        IIdentityService identity,
        IMemberDirectory members,
        IFileStorage storage,
        StorageLimits limits)
    {
        _currentUser = currentUser;
        _identity = identity;
        _members = members;
        _storage = storage;
        _limits = limits;
    }

    public async Task<MemberDto> Handle(UpdateMyPhotoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var file = request.File;
        var header = await FileBytes.ReadHeaderAsync(file.Content, FileSignatures.HeaderLength, cancellationToken);
        var extension = FileValidation.Validate(
            file.FileName, file.ContentType, file.Length, header, _limits.MaxAvatarBytes, AllowedFileTypes.Images);

        var key = StorageKeys.Avatar(userId, extension);

        // Remove the prior avatar only when the new key differs (a same-extension upload overwrites in place).
        var currentKey = await _members.Members
            .Where(m => m.Id == userId)
            .Select(m => m.PhotoUrl)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrEmpty(currentKey) && currentKey != key)
        {
            await _storage.DeleteAsync(currentKey, cancellationToken);
        }

        await _storage.SaveAsync(file.Content, key, AllowedFileTypes.Images[extension][0], cancellationToken);

        var result = await _identity.UpdatePhotoUrlAsync(userId, key, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? "The profile photo could not be updated.");
        }

        var view = await _members.Members.FirstOrDefaultAsync(m => m.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Member", userId);

        return view.ToDto();
    }
}
