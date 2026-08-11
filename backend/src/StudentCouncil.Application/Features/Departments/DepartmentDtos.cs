using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Departments;

public sealed record DepartmentLeadershipDto(
    MemberSummaryDto? President,
    MemberSummaryDto? VicePresident,
    MemberSummaryDto? Secretary);

public sealed record DepartmentDto(
    Guid Id,
    DepartmentCode Code,
    string Name,
    string? Description,
    int MemberCount,
    DepartmentLeadershipDto Leadership);

public sealed record DepartmentDetailDto(
    Guid Id,
    DepartmentCode Code,
    string Name,
    string? Description,
    int MemberCount,
    DepartmentLeadershipDto Leadership,
    IReadOnlyList<MemberSummaryDto> Members);

internal static class DepartmentMappings
{
    /// <summary>Builds the leadership summary from the department's (active) members.</summary>
    public static DepartmentLeadershipDto Leadership(IEnumerable<MemberView> members)
    {
        var list = members.ToList();

        MemberSummaryDto? Find(SystemRole role) =>
            list.FirstOrDefault(m => m.Role == role)?.ToSummary();

        return new DepartmentLeadershipDto(
            Find(SystemRole.DeptPresident),
            Find(SystemRole.DeptVicePresident),
            Find(SystemRole.DeptSecretary));
    }
}
