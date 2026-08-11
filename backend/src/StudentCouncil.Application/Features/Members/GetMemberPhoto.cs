using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Files;

namespace StudentCouncil.Application.Features.Members;

/// <summary>Streams a member's avatar (the public target of <c>MemberDto.PhotoUrl</c>).</summary>
public sealed record GetMemberPhotoQuery(Guid MemberId) : IRequest<StreamedFile>;

public sealed class GetMemberPhotoHandler : IRequestHandler<GetMemberPhotoQuery, StreamedFile>
{
    private readonly IMemberDirectory _members;
    private readonly IFileStorage _storage;

    public GetMemberPhotoHandler(IMemberDirectory members, IFileStorage storage)
    {
        _members = members;
        _storage = storage;
    }

    public async Task<StreamedFile> Handle(GetMemberPhotoQuery request, CancellationToken cancellationToken)
    {
        var key = await _members.Members
            .Where(m => m.Id == request.MemberId)
            .Select(m => m.PhotoUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(key))
        {
            throw new NotFoundException("Photo", request.MemberId);
        }

        var extension = Path.GetExtension(key);
        var contentType = AllowedFileTypes.Images.TryGetValue(extension, out var mimes)
            ? mimes[0]
            : "application/octet-stream";

        var stream = await _storage.OpenReadAsync(key, cancellationToken);
        return new StreamedFile(stream, contentType);
    }
}
