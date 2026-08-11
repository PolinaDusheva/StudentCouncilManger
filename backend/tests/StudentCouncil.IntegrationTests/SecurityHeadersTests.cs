using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class SecurityHeadersTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public SecurityHeadersTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Responses_carry_the_baseline_security_headers()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");

        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");

        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("no-referrer");
    }
}
