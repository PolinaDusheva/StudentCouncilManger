using MediatR;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Features.Events;

public sealed record GetEventsQuery(
    DateTime? From = null,
    DateTime? To = null,
    string? Type = null,
    string? Department = null,
    string? View = null) : IRequest<IReadOnlyList<EventDto>>;

public sealed class GetEventsHandler : IRequestHandler<GetEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public GetEventsHandler(IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var (from, to) = CalendarWindow.Resolve(request.View, request.From, request.To, _clock.UtcNow);

        return await CalendarReader.ReadAsync(
            _db, _members, _currentUser, from, to, request.Type, request.Department, cancellationToken);
    }
}
