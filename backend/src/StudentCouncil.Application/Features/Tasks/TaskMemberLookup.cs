using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>
/// Tasks-flavoured facade over the shared <see cref="MemberLookup"/> (decision #12). Kept so the
/// Phase 3 call sites read unchanged; both methods delegate to the common implementation.
/// </summary>
internal static class TaskMemberLookup
{
    public static Task<IReadOnlyDictionary<Guid, MemberSummaryDto>> LoadAsync(
        IMemberDirectory members, IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
        MemberLookup.LoadAsync(members, ids, cancellationToken);

    public static MemberSummaryDto? Find(this IReadOnlyDictionary<Guid, MemberSummaryDto> map, Guid id) =>
        MemberLookup.Find(map, id);
}
