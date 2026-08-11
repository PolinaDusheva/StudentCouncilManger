using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Tasks;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Duties;

public sealed record CreateDutyRecordCommand(
    Guid MemberId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Note) : IRequest<DutyRecordDto>, IDutyTimeInput;

public sealed class CreateDutyRecordValidator : AbstractValidator<CreateDutyRecordCommand>
{
    public CreateDutyRecordValidator(IMemberDirectory members)
    {
        DutyRecordRules.ApplyTimeAndNote(this);

        RuleFor(x => x.MemberId)
            .MustAsync((id, ct) => CreateTaskValidator.AllActiveMembersAsync(members, [id], ct))
            .WithMessage("The member must be an existing, active member.");
    }
}

public sealed class CreateDutyRecordHandler : IRequestHandler<CreateDutyRecordCommand, DutyRecordDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditRecorder _audit;

    public CreateDutyRecordHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, IAuditRecorder audit)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<DutyRecordDto> Handle(CreateDutyRecordCommand request, CancellationToken cancellationToken)
    {
        var recordedById = _currentUser.Id ?? throw new UnauthorizedException();

        var record = new DutyRecord
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            // Duration and reporting period are derived server-side, never taken from the client.
            DurationMinutes = (int)(request.EndUtc - request.StartUtc).TotalMinutes,
            PeriodYear = request.StartUtc.Year,
            PeriodMonth = request.StartUtc.Month,
            // RecordedById is the actor (decision #7); CreatedById is set identically by the interceptor.
            RecordedById = recordedById,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _db.DutyRecords.Add(record);
        _audit.Record(AuditActions.DutyRegistered, AuditEntities.DutyRecord, record.Id,
            new { record.MemberId, record.StartUtc, record.EndUtc, record.DurationMinutes });
        await _db.SaveChangesAsync(cancellationToken);

        return await DutyMappings.ToDtoAsync(_members, record, cancellationToken);
    }
}

/// <summary>Common time fields of the create/update duty inputs, so the rules can be written once.</summary>
internal interface IDutyTimeInput
{
    DateTime StartUtc { get; }
    DateTime EndUtc { get; }
    string? Note { get; }
}

/// <summary>Time + note rules shared by create and update (spec 11, plan 4.3.1).</summary>
internal static class DutyRecordRules
{
    public static void ApplyTimeAndNote<T>(AbstractValidator<T> validator)
        where T : IDutyTimeInput
    {
        validator.RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc).WithMessage("The end time must be after the start time.")
            .Must((command, end) => (end - command.StartUtc).TotalMinutes >= 1)
            .WithMessage("The duty must be at least one minute long.");

        validator.RuleFor(x => x.Note).MaximumLength(1000);
    }
}
