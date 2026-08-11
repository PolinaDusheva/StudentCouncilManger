using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Duties;

/// <summary>Body of <c>PUT /duty-records/{id}</c>; the member is fixed, only the shift can change.</summary>
public sealed record UpdateDutyRecordRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string? Note);

public sealed record UpdateDutyRecordCommand(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Note) : IRequest<DutyRecordDto>, IDutyTimeInput
{
    public static UpdateDutyRecordCommand From(Guid id, UpdateDutyRecordRequest request) =>
        new(id, request.StartUtc, request.EndUtc, request.Note);
}

public sealed class UpdateDutyRecordValidator : AbstractValidator<UpdateDutyRecordCommand>
{
    public UpdateDutyRecordValidator() => DutyRecordRules.ApplyTimeAndNote(this);
}

public sealed class UpdateDutyRecordHandler : IRequestHandler<UpdateDutyRecordCommand, DutyRecordDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly IAuditRecorder _audit;

    public UpdateDutyRecordHandler(IAppDbContext db, IMemberDirectory members, IAuditRecorder audit)
    {
        _db = db;
        _members = members;
        _audit = audit;
    }

    public async Task<DutyRecordDto> Handle(UpdateDutyRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _db.DutyRecords
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Duty record", request.Id);

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        // Diff the old values against the new ones before mutating (decision #4).
        var changes = new AuditDetails()
            .Change("startUtc", record.StartUtc, request.StartUtc)
            .Change("endUtc", record.EndUtc, request.EndUtc)
            .Change("note", record.Note, note);

        record.StartUtc = request.StartUtc;
        record.EndUtc = request.EndUtc;
        record.Note = note;

        // Recompute the derived duration and reporting period from the new times.
        record.DurationMinutes = (int)(request.EndUtc - request.StartUtc).TotalMinutes;
        record.PeriodYear = request.StartUtc.Year;
        record.PeriodMonth = request.StartUtc.Month;

        _audit.Record(AuditActions.DutyUpdated, AuditEntities.DutyRecord, record.Id, changes.Values);
        await _db.SaveChangesAsync(cancellationToken);

        return await DutyMappings.ToDtoAsync(_members, record, cancellationToken);
    }
}
