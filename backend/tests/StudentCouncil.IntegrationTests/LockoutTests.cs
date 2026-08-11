using System.Net;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class LockoutTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public LockoutTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Account_locks_after_five_failed_attempts()
    {
        var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var response = await Api.LoginRawAsync(client, SmokeTestFactory.AdminEmail, "WrongPass1");
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.Locked)
            {
                break;
            }
        }

        statuses.Should().Contain(HttpStatusCode.Locked); // 423
        statuses.TakeWhile(s => s != HttpStatusCode.Locked)
            .Should().OnlyContain(s => s == HttpStatusCode.Unauthorized);
        statuses.Count(s => s == HttpStatusCode.Unauthorized)
            .Should().BeGreaterThanOrEqualTo(4, "five failed attempts are required before lockout");
    }
}
