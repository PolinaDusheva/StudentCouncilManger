using Microsoft.EntityFrameworkCore;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// Persistence surface exposed to the Application layer. Hides the concrete
/// EF Core <c>DbContext</c> while still allowing LINQ queries over the sets.
/// (The Identity <c>ApplicationUser</c> set is intentionally not exposed here.)
/// </summary>
public interface IAppDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<TaskItem> Tasks { get; }
    DbSet<TaskAssignee> TaskAssignees { get; }
    DbSet<TaskComment> TaskComments { get; }
    DbSet<TaskDocument> TaskDocuments { get; }
    DbSet<CalendarEvent> CalendarEvents { get; }
    DbSet<EventParticipant> EventParticipants { get; }
    DbSet<DutyRecord> DutyRecords { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<JobRun> JobRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
