using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Fresh factory (database + rate-limit window) per test, matching the one-scenario-per-factory pattern.
public class BudgetTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Everyone_can_view_but_only_budget_managers_can_add()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        await TaskApi.CreateMemberAsync(client, "Viewer", "budget.viewer@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var create = await client.PostAsJsonAsync("/api/v1/budget/expenses", new
        {
            description = "Banner printing",
            amountEur = 100.50m,
            spentOn = "2026-06-01"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var expense = (await create.Content.ReadFromJsonAsync<ExpenseResponse>())!;
        expense.AddedBy.Should().NotBeNull();
        expense.AmountEur.Should().Be(100.50m);

        var summary = await client.GetFromJsonAsync<BudgetSummaryResponse>("/api/v1/budget/summary?year=2026");
        summary!.Year.Should().Be(2026);
        summary.TotalEur.Should().Be(100.50m);

        var expenses = await client.GetFromJsonAsync<PagedExpensesResponse>("/api/v1/budget/expenses?year=2026");
        expenses!.Items.Should().ContainSingle(e => e.Id == expense.Id);

        // A plain member can read the budget but not add to it (functional 9.2).
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "budget.viewer@ue-varna.bg", "ViewerPass1");
        (await client.GetAsync("/api/v1/budget/summary?year=2026")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/budget/expenses?year=2026")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/v1/budget/expenses", new
        {
            description = "Member tries to spend",
            amountEur = 10m,
            spentOn = "2026-06-01"
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_expenses_are_rejected()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        // Negative amount.
        (await client.PostAsJsonAsync("/api/v1/budget/expenses",
            new { description = "Negative", amountEur = -5m, spentOn = "2026-06-01" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // More than two decimal places.
        (await client.PostAsJsonAsync("/api/v1/budget/expenses",
            new { description = "Too precise", amountEur = 12.345m, spentOn = "2026-06-01" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Spend date in the future.
        (await client.PostAsJsonAsync("/api/v1/budget/expenses",
            new { description = "Future spend", amountEur = 10m, spentOn = "2099-01-01" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
