using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Application.Features.Members;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Duties;

public static class DutyMappings
{
    /// <summary>Projects a single record, batch-resolving its member and recorder summaries.</summary>
    public static async Task<DutyRecordDto> ToDtoAsync(
        IMemberDirectory members, DutyRecord record, CancellationToken cancellationToken)
    {
        var map = await MemberLookup.LoadAsync(members, [record.MemberId, record.RecordedById], cancellationToken);
        return Build(record, map);
    }

    public static DutyRecordDto Build(DutyRecord record, IReadOnlyDictionary<Guid, MemberSummaryDto> map) =>
        new(
            record.Id,
            map.Find(record.MemberId),
            record.StartUtc,
            record.EndUtc,
            record.DurationMinutes,
            record.PeriodYear,
            record.PeriodMonth,
            map.Find(record.RecordedById),
            record.Note);
}

/// <summary>Aggregates duty minutes per member for a reporting month (shared by summary + remind).</summary>
internal static class DutyTotals
{
    public static async Task<IReadOnlyDictionary<Guid, int>> ByMemberAsync(
        IAppDbContext db, int year, int month, CancellationToken cancellationToken)
    {
        var totals = await db.DutyRecords
            .AsNoTracking()
            .Where(d => d.PeriodYear == year && d.PeriodMonth == month)
            .GroupBy(d => d.MemberId)
            .Select(g => new { MemberId = g.Key, Total = g.Sum(d => d.DurationMinutes) })
            .ToListAsync(cancellationToken);

        return totals.ToDictionary(t => t.MemberId, t => t.Total);
    }
}
