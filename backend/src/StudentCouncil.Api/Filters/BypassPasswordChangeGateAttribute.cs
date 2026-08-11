namespace StudentCouncil.Api.Filters;

/// <summary>
/// Marks an action as reachable even when the caller still has <c>MustChangePassword = true</c>
/// (e.g. change-password, me, logout). See <see cref="RequirePasswordChangeFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class BypassPasswordChangeGateAttribute : Attribute;
