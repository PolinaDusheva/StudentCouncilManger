using System.Net;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class RateLimitTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public RateLimitTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Auth_endpoints_return_429_past_the_limit()
    {
        var client = _factory.CreateClient();

        var sawTooManyRequests = false;
        for (var i = 0; i < 15 && !sawTooManyRequests; i++)
        {
            // Distinct unknown emails → 401 until the per-IP auth limit (10/min) kicks in.
            var response = await Api.LoginRawAsync(client, $"ghost{i}@ue-varna.bg", "WrongPass1");
            sawTooManyRequests = response.StatusCode == HttpStatusCode.TooManyRequests;
        }

        sawTooManyRequests.Should().BeTrue("the auth endpoints should be rate limited");
    }
}
