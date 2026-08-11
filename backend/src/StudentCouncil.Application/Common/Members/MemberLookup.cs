using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Common.Members;

/// <summary>
/// Resolves member ids to <see cref="MemberSummaryDto"/> in a single batched query (decision #12).
/// Calendar, duty and budget entities — like tasks — reference members by plain <c>Guid</c> with no
/// EF navigation to the Identity <c>ApplicationUser</c>, so detail DTOs are stitched together in a
/// second step. Shared so every module batches the same way (<see cref="TaskMemberLookup"/> delegates here).
/// </summary>
public static class MemberLookup
{
    public static async Task<IReadOnlyDictionary<Guid, MemberSummaryDto>> LoadAsync(
        IMemberDirectory members, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, MemberSummaryDto>();
        }

        var views = await members.Members
            .Where(m => distinct.Contains(m.Id))
            .ToListAsync(cancellationToken);

        return views.ToDictionary(v => v.Id, v => v.ToSummary());
    }

    /// <summary>Looks up a member summary, returning <c>null</c> when the id no longer resolves.</summary>
    public static MemberSummaryDto? Find(this IReadOnlyDictionary<Guid, MemberSummaryDto> map, Guid id) =>
        map.TryGetValue(id, out var summary) ? summary : null;
}
