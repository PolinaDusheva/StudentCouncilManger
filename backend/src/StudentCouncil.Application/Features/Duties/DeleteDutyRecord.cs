using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Duties;

/// <summary>Physically deletes a duty record (not soft-deletable; a plain remove is a true delete).</summary>
public sealed record DeleteDutyRecordCommand(Guid Id) : IRequest;

public sealed class DeleteDutyRecordHandler : IRequestHandler<DeleteDutyRecordCommand>
{
    private readonly IAppDbContext _db;
    private readonly IAuditRecorder _audit;

    public DeleteDutyRecordHandler(IAppDbContext db, IAuditRecorder audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task Handle(DeleteDutyRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _db.DutyRecords
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Duty record", request.Id);

        _audit.Record(AuditActions.DutyDeleted, AuditEntities.DutyRecord, record.Id,
            new { record.MemberId, record.PeriodYear, record.PeriodMonth });

        _db.DutyRecords.Remove(record);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
