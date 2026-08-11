using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Features.Members;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Members;

public class DeactivateMemberHandlerTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IRefreshTokenService _refreshTokens = Substitute.For<IRefreshTokenService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IAuditRecorder _audit = Substitute.For<IAuditRecorder>();

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"deactivate-{Guid.NewGuid()}").Options);

    private static async Task<ApplicationUser> AddUserAsync(AppDbContext db, SystemRole role)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@ue-varna.bg",
            FullName = "Member",
            Role = role,
            Status = MemberStatus.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Deactivates_and_revokes_tokens()
    {
        await using var db = NewDb();
        var target = await AddUserAsync(db, SystemRole.Member);
        _currentUser.Id.Returns(Guid.NewGuid());
        _identity.SetStatusAsync(target.Id, MemberStatus.Inactive, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new DeactivateMemberHandler(new MemberDirectory(db), _identity, _refreshTokens, _currentUser, _audit, db);
        await sut.Handle(new DeactivateMemberCommand(target.Id), default);

        await _identity.Received().SetStatusAsync(target.Id, MemberStatus.Inactive, Arg.Any<CancellationToken>());
        await _refreshTokens.Received().RevokeAllForMemberAsync(target.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cannot_deactivate_yourself()
    {
        await using var db = NewDb();
        var target = await AddUserAsync(db, SystemRole.OrgVicePresident);
        _currentUser.Id.Returns(target.Id);

        var sut = new DeactivateMemberHandler(new MemberDirectory(db), _identity, _refreshTokens, _currentUser, _audit, db);
        var act = () => sut.Handle(new DeactivateMemberCommand(target.Id), default);

        await act.Should().ThrowAsync<ConflictException>();
        await _identity.DidNotReceive().SetStatusAsync(Arg.Any<Guid>(), Arg.Any<MemberStatus>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cannot_deactivate_the_last_active_org_president()
    {
        await using var db = NewDb();
        var president = await AddUserAsync(db, SystemRole.OrgPresident);
        _currentUser.Id.Returns(Guid.NewGuid());

        var sut = new DeactivateMemberHandler(new MemberDirectory(db), _identity, _refreshTokens, _currentUser, _audit, db);
        var act = () => sut.Handle(new DeactivateMemberCommand(president.Id), default);

        await act.Should().ThrowAsync<ConflictException>();
        await _identity.DidNotReceive().SetStatusAsync(Arg.Any<Guid>(), Arg.Any<MemberStatus>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_deactivating_a_president_when_another_remains()
    {
        await using var db = NewDb();
        var president = await AddUserAsync(db, SystemRole.OrgPresident);
        await AddUserAsync(db, SystemRole.OrgPresident); // a second active president
        _currentUser.Id.Returns(Guid.NewGuid());
        _identity.SetStatusAsync(president.Id, MemberStatus.Inactive, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = new DeactivateMemberHandler(new MemberDirectory(db), _identity, _refreshTokens, _currentUser, _audit, db);
        await sut.Handle(new DeactivateMemberCommand(president.Id), default);

        await _identity.Received().SetStatusAsync(president.Id, MemberStatus.Inactive, Arg.Any<CancellationToken>());
    }
}
