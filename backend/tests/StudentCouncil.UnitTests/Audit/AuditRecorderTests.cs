using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Infrastructure.Audit;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Audit;

public class AuditRecorderTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"audit-{Guid.NewGuid()}").Options);

    private static IAuditRecorder NewRecorder(AppDbContext db, Guid? actorId, DateTime now)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(actorId);

        var clock = Substitute.For<IDateTime>();
        clock.UtcNow.Returns(now);

        return new AuditRecorder(db, currentUser, clock, NullLogger<AuditRecorder>.Instance);
    }

    [Fact]
    public async Task Record_stages_a_row_with_actor_action_and_timestamp()
    {
        await using var db = NewDb();
        var actorId = Guid.NewGuid();
        var now = new DateTime(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc);
        var recorder = NewRecorder(db, actorId, now);

        var entityId = Guid.NewGuid();
        recorder.Record(AuditActions.ExpenseAdded, AuditEntities.Expense, entityId,
            new { amountEur = 12.50m, description = "Markers" });

        // Record only stages — the handler's SaveChanges is what commits it (decision #1).
        (await db.AuditLogs.CountAsync()).Should().Be(0);
        await db.SaveChangesAsync();

        var row = await db.AuditLogs.SingleAsync();
        row.ActorId.Should().Be(actorId);
        row.Action.Should().Be(AuditActions.ExpenseAdded);
        row.EntityType.Should().Be(AuditEntities.Expense);
        row.EntityId.Should().Be(entityId.ToString());
        row.Timestamp.Should().Be(now);
        row.Details.Should().NotBeNull();
        row.Details!.Should().Contain("amountEur").And.Contain("Markers");
    }

    [Fact]
    public async Task Record_serialises_null_details_when_omitted()
    {
        await using var db = NewDb();
        var recorder = NewRecorder(db, Guid.NewGuid(), DateTime.UtcNow);

        // Exercises the Guid convenience overload as well.
        recorder.Record(AuditActions.MemberReactivated, AuditEntities.Member, Guid.NewGuid());
        await db.SaveChangesAsync();

        (await db.AuditLogs.SingleAsync()).Details.Should().BeNull();
    }

    [Fact]
    public async Task Record_without_actor_does_not_throw_or_stage()
    {
        await using var db = NewDb();
        var recorder = NewRecorder(db, actorId: null, DateTime.UtcNow);

        var act = () => recorder.Record(AuditActions.TaskDeleted, AuditEntities.TaskItem, Guid.NewGuid());

        act.Should().NotThrow();
        await db.SaveChangesAsync();
        (await db.AuditLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Diff_records_only_changed_fields_as_from_to_pairs()
    {
        var diff = new AuditDetails()
            .Change("amountEur", 10m, 12m)
            .Change("description", "same", "same");

        diff.HasChanges.Should().BeTrue();
        diff.Values.Should().ContainKey("amountEur");
        diff.Values.Should().NotContainKey("description");
    }
}
