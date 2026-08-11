using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Devices;

/// <summary>
/// Removes a push token at logout (spec 7.9). Idempotent: only the caller's own token is deleted; an
/// unknown or foreign token is a silent no-op, so the endpoint never reveals another user's tokens.
/// </summary>
public sealed record DeregisterDeviceCommand(string Token) : IRequest;

public sealed class DeregisterDeviceHandler : IRequestHandler<DeregisterDeviceCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeregisterDeviceHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var token = request.Token.Trim();

        var device = await _db.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == token && d.MemberId == userId, cancellationToken);
        if (device is null)
        {
            return;
        }

        _db.DeviceTokens.Remove(device);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
