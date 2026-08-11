namespace StudentCouncil.Application.Features.Events;

/// <summary>
/// Resolves the <c>[from, to)</c> time window for the calendar endpoints. Explicit <c>from</c>/<c>to</c>
/// win; anything missing falls back to a default derived from <c>view</c> (month/week/day/list).
/// </summary>
internal static class CalendarWindow
{
    public static (DateTime From, DateTime To) Resolve(string? view, DateTime? from, DateTime? to, DateTime nowUtc)
    {
        var (defaultFrom, defaultTo) = DefaultWindow(view, nowUtc);
        return (from ?? defaultFrom, to ?? defaultTo);
    }

    private static (DateTime From, DateTime To) DefaultWindow(string? view, DateTime now) =>
        view?.ToLowerInvariant() switch
        {
            "month" => MonthWindow(now),
            "week" => WeekWindow(now),
            "day" => DayWindow(now),
            // "list" and any unrecognised view: a rolling 90-day look-ahead from now.
            _ => (now, now.AddDays(90))
        };

    private static (DateTime, DateTime) MonthWindow(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }

    private static (DateTime, DateTime) WeekWindow(DateTime now)
    {
        // Week starts Monday (European convention): map Mon..Sun to 0..6.
        var offset = ((int)now.DayOfWeek + 6) % 7;
        var start = now.Date.AddDays(-offset);
        return (start, start.AddDays(7));
    }

    private static (DateTime, DateTime) DayWindow(DateTime now)
    {
        var start = now.Date;
        return (start, start.AddDays(1));
    }
}
