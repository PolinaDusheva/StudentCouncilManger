using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCouncil.Application.Features.Devices;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/devices")]
[Authorize]
public sealed class DevicesController : ControllerBase
{
    private readonly ISender _sender;

    public DevicesController(ISender sender) => _sender = sender;

    /// <summary>Registers (or refreshes) the caller's push token (upsert keyed on the token).</summary>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterDeviceCommand(request.Token, request.Platform), cancellationToken);

        return result.Created
            ? Created($"/api/v1/devices/{result.Id}", result)
            : Ok(result);
    }

    /// <summary>Deregisters a token at logout (idempotent; only the caller's own token is removed).</summary>
    [HttpDelete("{token}")]
    public async Task<IActionResult> Deregister(string token, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeregisterDeviceCommand(token), cancellationToken);
        return NoContent();
    }
}
