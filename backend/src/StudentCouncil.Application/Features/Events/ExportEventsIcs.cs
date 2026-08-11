using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Calendar;

namespace StudentCouncil.Application.Features.Events;

public sealed record ExportEventsIcsQuery(
    DateTime? From = null,
    DateTime? To = null,
    string? Type = null,
    string? Department = null,
    string? View = null) : IRequest<string>;

public sealed class ExportEventsIcsHandler : IRequestHandler<ExportEventsIcsQuery, string>
{
    private const string Domain = "studentcouncil.ue-varna.bg";

    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public ExportEventsIcsHandler(IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<string> Handle(ExportEventsIcsQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var (from, to) = CalendarWindow.Resolve(request.View, request.From, request.To, now);

        var events = await CalendarReader.ReadAsync(
            _db, _members, _currentUser, from, to, request.Type, request.Department, cancellationToken);

        var icsEvents = events.Select(ToIcsEvent);
        return IcsBuilder.Build(icsEvents, now);
    }

    private static IcsEvent ToIcsEvent(EventDto e) =>
        new(Uid(e), e.StartUtc, e.EndUtc, e.Title, e.Description, e.Location);

    private static string Uid(EventDto e)
    {
        if (e.IsDeadline)
        {
            return $"task-{e.TaskId}@{Domain}";
        }

        return e.OccurrenceStartUtc is { } occurrence
            ? $"{e.Id}_{occurrence:yyyyMMdd}@{Domain}"
            : $"{e.Id}@{Domain}";
    }
}
