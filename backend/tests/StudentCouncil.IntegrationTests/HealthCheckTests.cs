using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class HealthCheckTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public HealthCheckTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Live_endpoint_is_healthy_without_dependencies()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Healthy");
    }

    [Fact]
    public async Task Ready_endpoint_reports_per_check_results()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");

        var results = doc.RootElement.GetProperty("results");
        results.TryGetProperty("database", out _).Should().BeTrue();
        results.TryGetProperty("blob", out _).Should().BeTrue();
        results.TryGetProperty("push", out _).Should().BeTrue();
    }
}
