namespace StudentCouncil.Application.Abstractions;

/// <summary>Ambient information about the caller, resolved from the JWT.</summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    string? Email { get; }
    SystemRole? Role { get; }
    Guid? DepartmentId { get; }
    bool IsAuthenticated { get; }
}
