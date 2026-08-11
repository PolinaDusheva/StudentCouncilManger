using FluentAssertions;
using StudentCouncil.Application.Common.Calendar;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Calendar;

public class RecurrenceExpanderTests
{
    private static CalendarEvent Event(DateTime startUtc, TimeSpan duration, RecurrenceType recurrence) =>
        new() { Id = Guid.NewGuid(), StartUtc = startUtc, EndUtc = startUtc + duration, Recurrence = recurrence };

    private static DateTime Utc(int y, int m, int d, int h = 0) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void None_inside_window_yields_a_single_instance()
    {
        var calendarEvent = Event(Utc(2026, 6, 10, 9), TimeSpan.FromHours(2), RecurrenceType.None);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 6, 1), Utc(2026, 6, 30)).ToList();

        occurrences.Should().ContainSingle();
        occurrences[0].StartUtc.Should().Be(Utc(2026, 6, 10, 9));
    }

    [Fact]
    public void None_outside_window_yields_nothing()
    {
        var calendarEvent = Event(Utc(2026, 6, 10, 9), TimeSpan.FromHours(2), RecurrenceType.None);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 7, 1), Utc(2026, 7, 31)).ToList();

        occurrences.Should().BeEmpty();
    }

    [Fact]
    public void None_spanning_the_window_boundary_is_included()
    {
        // Starts before the window but ends inside it -> overlaps -> included.
        var calendarEvent = Event(Utc(2026, 5, 31, 23), TimeSpan.FromHours(2), RecurrenceType.None);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 6, 1), Utc(2026, 6, 30)).ToList();

        occurrences.Should().ContainSingle();
    }

    [Fact]
    public void Weekly_yields_one_occurrence_per_week_in_the_window()
    {
        var calendarEvent = Event(Utc(2026, 6, 1, 9), TimeSpan.FromHours(1), RecurrenceType.Weekly);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 6, 1), Utc(2026, 6, 30)).ToList();

        // Jun 1, 8, 15, 22, 29 — the Jul 6 occurrence falls outside the [from, to) window.
        occurrences.Should().HaveCount(5);
        occurrences.Select(o => o.StartUtc.Day).Should().Equal(1, 8, 15, 22, 29);
    }

    [Fact]
    public void Monthly_yields_one_occurrence_per_month_in_the_window()
    {
        var calendarEvent = Event(Utc(2026, 1, 15, 10), TimeSpan.FromHours(2), RecurrenceType.Monthly);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 1, 1), Utc(2026, 6, 30)).ToList();

        occurrences.Should().HaveCount(6);
        occurrences.Select(o => o.StartUtc.Month).Should().Equal(1, 2, 3, 4, 5, 6);
    }

    [Fact]
    public void Recurring_event_starting_far_in_the_past_still_lands_in_the_window()
    {
        var calendarEvent = Event(Utc(2020, 1, 1, 9), TimeSpan.FromHours(1), RecurrenceType.Weekly);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 6, 1), Utc(2026, 6, 8)).ToList();

        occurrences.Should().ContainSingle();
        occurrences[0].StartUtc.Should().BeOnOrAfter(Utc(2026, 6, 1)).And.BeBefore(Utc(2026, 6, 8));
    }

    [Fact]
    public void Duration_is_preserved_across_occurrences()
    {
        var duration = TimeSpan.FromMinutes(90);
        var calendarEvent = Event(Utc(2026, 6, 1, 9), duration, RecurrenceType.Weekly);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 6, 1), Utc(2026, 6, 30)).ToList();

        occurrences.Should().OnlyContain(o => o.EndUtc - o.StartUtc == duration);
    }

    [Fact]
    public void Cap_limits_the_number_of_occurrences()
    {
        var calendarEvent = Event(Utc(2026, 1, 1, 9), TimeSpan.FromHours(1), RecurrenceType.Weekly);

        var occurrences = RecurrenceExpander.Expand(calendarEvent, Utc(2026, 1, 1), Utc(2027, 1, 1), cap: 3).ToList();

        occurrences.Should().HaveCount(3);
    }
}
