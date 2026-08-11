using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>
/// EF Core-backed read access to members. Projects <see cref="ApplicationUser"/> to the
/// Identity-free <see cref="MemberView"/> so the Application layer can compose queries.
/// </summary>
public sealed class MemberDirectory : IMemberDirectory
{
    private readonly AppDbContext _db;

    public MemberDirectory(AppDbContext db) => _db = db;

    public IQueryable<MemberView> Members => _db.Users
        .AsNoTracking()
        .Select(u => new MemberView
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email!,
            PhoneNumber = u.PhoneNumber,
            PhotoUrl = u.PhotoUrl,
            Role = u.Role,
            DepartmentId = u.DepartmentId,
            DepartmentCode = u.Department != null ? u.Department.Code : (DepartmentCode?)null,
            DepartmentName = u.Department != null ? u.Department.Name : null,
            JoinedOn = u.JoinedOn,
            Status = u.Status
        });
}
