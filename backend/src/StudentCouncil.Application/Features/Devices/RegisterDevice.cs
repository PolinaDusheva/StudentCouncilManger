using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Devices;

/// <summary>
/// Registers (or refreshes) a push token for the current user. Upsert is keyed on the token itself, so a
/// device that moves between accounts re-binds to the caller — a token is always tied to the authenticated
/// user, never to an id supplied by the client (spec 7.9, plan 4.3).
/// </summary>
public sealed record RegisterDeviceCommand(string Token, DevicePlatform Platform) : IRequest<DeviceRegistrationResult>;

public sealed class RegisterDeviceValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).IsInEnum();
    }
}

public sealed class RegisterDeviceHandler : IRequestHandler<RegisterDeviceCommand, DeviceRegistrationResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public RegisterDeviceHandler(IAppDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DeviceRegistrationResult> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var token = request.Token.Trim();
        var now = _clock.UtcNow;

        var existing = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token, cancellationToken);
        if (existing is not null)
        {
            existing.MemberId = userId;
            existing.Platform = request.Platform;
            existing.LastSeenUtc = now;
            await _db.SaveChangesAsync(cancellationToken);
            return new DeviceRegistrationResult(existing.Id, Created: false);
        }

        var device = new DeviceToken
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            Token = token,
            Platform = request.Platform,
            CreatedAtUtc = now,
            LastSeenUtc = now
        };

        _db.DeviceTokens.Add(device);
        await _db.SaveChangesAsync(cancellationToken);
        return new DeviceRegistrationResult(device.Id, Created: true);
    }
}
