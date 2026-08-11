using MediatR;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Features.Events;

public sealed record GetEventQuery(Guid Id) : IRequest<EventDetailDto>;

public sealed class GetEventHandler : IRequestHandler<GetEventQuery, EventDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;

    public GetEventHandler(IAppDbContext db, IMemberDirectory members)
    {
        _db = db;
        _members = members;
    }

    // Every authenticated member may see every event (decision #1), so there is no visibility gate
    // here — a missing event is a genuine 404, raised by the builder.
    public Task<EventDetailDto> Handle(GetEventQuery request, CancellationToken cancellationToken) =>
        EventDetailBuilder.LoadAndBuildAsync(_db, _members, request.Id, cancellationToken);
}
