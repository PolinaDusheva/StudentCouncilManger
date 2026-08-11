using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Common.Calendar;

/// <summary>
/// Expands a recurring <see cref="CalendarEvent"/> into the concrete occurrences that overlap a
/// requested <c>[from, to)</c> window (decision #3). The model has no recurrence-end field, so the
/// window plus a hard <paramref name="cap"/> bound the output — no schema change needed. The base
/// event is a single row; occurrences carry the stepped start/end and preserve the duration.
/// A pure function, isolated so it unit-tests cleanly.
/// </summary>
public static class RecurrenceExpander
{
    public static IEnumerable<(DateTime StartUtc, DateTime EndUtc)> Expand(
        CalendarEvent calendarEvent, DateTime windowFrom, DateTime windowTo, int cap = 366)
    {
        var duration = calendarEvent.EndUtc - calendarEvent.StartUtc;

        if (calendarEvent.Recurrence == RecurrenceType.None)
        {
            if (Overlaps(calendarEvent.StartUtc, calendarEvent.EndUtc, windowFrom, windowTo))
            {
                yield return (calendarEvent.StartUtc, calendarEvent.EndUtc);
            }

            yield break;
        }

        // Jump close to the window before iterating so the cap budgets occurrences inside the window,
        // not the (potentially years of) steps between the base start and the window.
        var start = FastForward(calendarEvent.StartUtc, calendarEvent.Recurrence, windowFrom);
        var produced = 0;

        while (start < windowTo && produced < cap)
        {
            var end = start + duration;
            if (end > windowFrom)
            {
                yield return (start, end);
                produced++;
            }

            start = Step(start, calendarEvent.Recurrence);
        }
    }

    private static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) =>
        aStart < bEnd && aEnd > bStart;

    private static DateTime Step(DateTime start, RecurrenceType recurrence) => recurrence switch
    {
        RecurrenceType.Weekly => start.AddDays(7),
        // AddMonths normalises the day towards the end of shorter months (e.g. 31 Jan -> 28/29 Feb).
        RecurrenceType.Monthly => start.AddMonths(1),
        _ => DateTime.MaxValue
    };

    /// <summary>Advances the base start to just at/before the window so the loop skips minimally.</summary>
    private static DateTime FastForward(DateTime start, RecurrenceType recurrence, DateTime windowFrom)
    {
        if (start >= windowFrom)
        {
            return start;
        }

        switch (recurrence)
        {
            case RecurrenceType.Weekly:
                var weeks = (long)Math.Floor((windowFrom - start).TotalDays / 7);
                return weeks > 0 ? start.AddDays(weeks * 7) : start;

            case RecurrenceType.Monthly:
                // Approximate the month count, then step back one so day-of-month normalisation
                // can never overshoot past the first overlapping occurrence.
                var months = ((windowFrom.Year - start.Year) * 12) + (windowFrom.Month - start.Month) - 1;
                return months > 0 ? start.AddMonths(months) : start;

            default:
                return start;
        }
    }
}
