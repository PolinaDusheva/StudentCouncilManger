namespace StudentCouncil.Application.Features.Devices;

/// <summary>Body of <c>POST /devices</c>: the push token and the platform it came from.</summary>
public sealed record RegisterDeviceRequest(string Token, DevicePlatform Platform);

/// <summary>Outcome of a device registration (whether a new row was created vs. an existing one refreshed).</summary>
public sealed record DeviceRegistrationResult(Guid Id, bool Created);
