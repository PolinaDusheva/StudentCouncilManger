using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Data-driven authorization matrix (decision #11, spec 17): every policy-protected endpoint is exercised by
// every role. Unauthorized roles must get 403 and anonymous callers 401; an allowed role must NOT be blocked
// by authorization (it may then hit validation/404 on a synthetic id, but never 403). Resource-level rules
// (own-department, visibility, owner-only) are covered by the dedicated *AuthorizationTests classes; the few
// scope negatives the matrix can assert directly live in the second test.
public class AuthorizationMatrixTests : IAsyncLifetime
{
    private readonly SmokeTestFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private sealed record ProtectedEndpoint(string Name, string Method, string Path, object? Body, string[] AllowedRoles);

    [Fact]
    public async Task Every_policy_protected_endpoint_enforces_its_role_gate()
    {
        var client = _factory.CreateClient();
        var seeded = await RoleSeeding.SeedAsync(_factory.Services);
        var primary = seeded.PrimaryDepartmentId;
        var assignee = seeded.MemberIdsByRole[RoleSeeding.Member];

        var orgLeadership = new[] { RoleSeeding.OrgPresident, RoleSeeding.OrgVicePresident };
        var deptTaskCreators = new[]
        {
            RoleSeeding.DeptPresident, RoleSeeding.DeptVicePresident,
            RoleSeeding.OrgPresident, RoleSeeding.OrgVicePresident
        };
        var orgPresidentOnly = new[] { RoleSeeding.OrgPresident };
        var eventManagers = RoleSeeding.AllRoles.Where(r => r != RoleSeeding.Member).ToArray();
        var random = Guid.NewGuid();

        object memberBody = new
        {
            fullName = "Matrix Created", email = "matrix.created@ue-varna.bg", phoneNumber = (string?)null,
            role = "Member", departmentId = primary, joinedOn = "2026-02-01"
        };
        object taskBody = new
        {
            title = "Matrix task", description = (string?)null, priority = "Low", scope = "Departmental",
            departmentId = primary, dueAtUtc = "2026-12-01T10:00:00Z", assigneeIds = new[] { assignee }
        };
        object dutyBody = new { memberId = assignee, startUtc = "2026-06-10T09:00:00Z", endUtc = "2026-06-10T11:00:00Z", note = (string?)null };
        object expenseBody = new { description = "Matrix expense", amountEur = 10m, spentOn = "2026-06-01" };
        object eventBody = new
        {
            title = "Matrix event", description = (string?)null, startUtc = "2026-07-10T09:00:00Z",
            endUtc = "2026-07-10T10:00:00Z", location = (string?)null, type = "Meeting",
            departmentId = primary, recurrence = "None"
        };

        var endpoints = new List<ProtectedEndpoint>
        {
            new("members.create", "POST", "/api/v1/members", memberBody, orgLeadership),
            new("members.update", "PUT", $"/api/v1/members/{random}", memberBody, orgLeadership),
            new("members.deactivate", "POST", $"/api/v1/members/{random}/deactivate", null, orgLeadership),
            new("members.reactivate", "POST", $"/api/v1/members/{random}/reactivate", null, orgLeadership),
            new("tasks.create", "POST", "/api/v1/tasks", taskBody, deptTaskCreators),
            new("tasks.update", "PUT", $"/api/v1/tasks/{random}", taskBody, deptTaskCreators),
            new("tasks.delete", "DELETE", $"/api/v1/tasks/{random}", null, orgPresidentOnly),
            new("duties.create", "POST", "/api/v1/duty-records", dutyBody, orgLeadership),
            new("duties.update", "PUT", $"/api/v1/duty-records/{random}", dutyBody, orgLeadership),
            new("duties.delete", "DELETE", $"/api/v1/duty-records/{random}", null, orgLeadership),
            new("duties.summary", "GET", "/api/v1/duty-records/summary?year=2026&month=6", null, orgLeadership),
            new("duties.remind", "POST", "/api/v1/duty-records/remind", new { }, orgLeadership),
            new("budget.create", "POST", "/api/v1/budget/expenses", expenseBody, orgLeadership),
            new("budget.update", "PUT", $"/api/v1/budget/expenses/{random}", expenseBody, orgLeadership),
            new("budget.delete", "DELETE", $"/api/v1/budget/expenses/{random}", null, orgLeadership),
            new("events.create", "POST", "/api/v1/events", eventBody, eventManagers),
            new("events.update", "PUT", $"/api/v1/events/{random}", eventBody, eventManagers),
            new("events.delete", "DELETE", $"/api/v1/events/{random}", null, eventManagers)
        };

        var failures = new List<string>();
        foreach (var endpoint in endpoints)
        {
            foreach (var role in RoleSeeding.AllRoles)
            {
                Api.UseBearer(client, seeded.TokensByRole[role]);
                var status = (await SendAsync(client, endpoint)).StatusCode;

                if (endpoint.AllowedRoles.Contains(role))
                {
                    if (status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
                    {
                        failures.Add($"{endpoint.Name}: {role} is authorized but was blocked with {(int)status}.");
                    }
                }
                else if (status != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{endpoint.Name}: {role} is not authorized but got {(int)status} instead of 403.");
                }
            }

            Api.ClearBearer(client);
            var anonymous = (await SendAsync(client, endpoint)).StatusCode;
            if (anonymous != HttpStatusCode.Unauthorized)
            {
                failures.Add($"{endpoint.Name}: anonymous got {(int)anonymous} instead of 401.");
            }
        }

        failures.Should().BeEmpty();
    }

    [Fact]
    public async Task Department_leadership_cannot_create_tasks_outside_its_scope()
    {
        var client = _factory.CreateClient();
        var seeded = await RoleSeeding.SeedAsync(_factory.Services);
        var assignee = seeded.MemberIdsByRole[RoleSeeding.Member];

        Api.UseBearer(client, seeded.TokensByRole[RoleSeeding.DeptPresident]);

        // An organisational task is OrgLeadership-only — the policy lets the dept president past the gate,
        // the handler's scope rule then rejects it (spec 7.4). A valid assignee clears validation first.
        var orgTask = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Org task by dept lead", description = (string?)null, priority = "Low",
            scope = "Organizational", departmentId = (Guid?)null, dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assignee }
        });
        orgTask.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // A departmental task for another department is also out of scope.
        var foreignTask = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Other dept task", description = (string?)null, priority = "Low",
            scope = "Departmental", departmentId = seeded.OtherDepartmentId, dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assignee }
        });
        foreignTask.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, ProtectedEndpoint endpoint) => endpoint.Method switch
    {
        "GET" => client.GetAsync(endpoint.Path),
        "DELETE" => client.DeleteAsync(endpoint.Path),
        "POST" => client.PostAsJsonAsync(endpoint.Path, endpoint.Body ?? new { }),
        "PUT" => client.PutAsJsonAsync(endpoint.Path, endpoint.Body ?? new { }),
        _ => throw new InvalidOperationException($"Unsupported method {endpoint.Method}.")
    };
}
