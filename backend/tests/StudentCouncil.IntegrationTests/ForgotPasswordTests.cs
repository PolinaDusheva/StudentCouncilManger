using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class ForgotPasswordTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public ForgotPasswordTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Returns_200_regardless_of_whether_the_email_exists()
    {
        var client = _factory.CreateClient();

        var existing = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = SmokeTestFactory.AdminEmail });
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = "does-not-exist@ue-varna.bg" });

        existing.StatusCode.Should().Be(HttpStatusCode.OK);
        unknown.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
