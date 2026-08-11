using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StudentCouncil.Api.Middleware;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.UnitTests.Api;

public class ExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, DefaultHttpContext Context, MemoryStream Body) Create(
        string environmentName = "Production")
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var handler = new GlobalExceptionHandler(environment, NullLogger<GlobalExceptionHandler>.Instance);

        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        return (handler, context, body);
    }

    public static IEnumerable<object[]> ExceptionCases() =>
    [
        [new ValidationException(), 400, "validation_error"],
        [new UnauthorizedException(), 401, "unauthorized"],
        [new ForbiddenException(), 403, "forbidden"],
        [new NotFoundException(), 404, "not_found"],
        [new ConflictException(), 409, "conflict"],
        [new InvalidOperationException("boom"), 500, "internal_error"]
    ];

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public async Task Maps_exception_to_status_and_problem_details(Exception exception, int status, string code)
    {
        var (handler, context, body) = Create();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(status);

        using var doc = JsonDocument.Parse(ReadBody(body));
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(status);
        doc.RootElement.GetProperty("code").GetString().Should().Be(code);
        doc.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validation_exception_serializes_field_errors()
    {
        var (handler, context, body) = Create();
        var exception = new ValidationException(new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required."]
        });

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.StatusCode.Should().Be(400);
        using var doc = JsonDocument.Parse(ReadBody(body));
        doc.RootElement.GetProperty("errors").GetProperty("Email")[0].GetString()
            .Should().Be("Email is required.");
    }

    [Fact]
    public async Task Server_error_does_not_leak_details_outside_development()
    {
        var (handler, context, body) = Create(environmentName: "Production");

        await handler.TryHandleAsync(context, new InvalidOperationException("secret"), CancellationToken.None);

        var json = ReadBody(body);
        json.Should().NotContain("secret");
    }

    private static string ReadBody(MemoryStream body)
    {
        body.Position = 0;
        return new StreamReader(body).ReadToEnd();
    }
}
