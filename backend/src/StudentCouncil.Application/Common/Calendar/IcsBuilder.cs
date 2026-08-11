using System.Globalization;
using System.Text;

namespace StudentCouncil.Application.Common.Calendar;

/// <summary>One calendar entry to serialise; the caller decides the UID scheme.</summary>
public sealed record IcsEvent(
    string Uid,
    DateTime StartUtc,
    DateTime EndUtc,
    string Summary,
    string? Description,
    string? Location);

/// <summary>
/// Minimal RFC 5545 (.ics) writer with no external dependency (decision #6). Emits one VCALENDAR
/// containing a VEVENT per entry; all times are UTC (<c>yyyyMMddTHHmmssZ</c>). UID schemes are the
/// caller's job — real events <c>{id}@…</c>, recurring occurrences <c>{id}_{yyyyMMdd}@…</c>, task
/// deadlines <c>task-{taskId}@…</c>.
/// </summary>
public static class IcsBuilder
{
    private const string ProductId = "-//Student Council UE-Varna//Calendar//EN";
    private const string LineBreak = "\r\n";

    public static string Build(IEnumerable<IcsEvent> events, DateTime nowUtc)
    {
        var stamp = FormatUtc(nowUtc);
        var builder = new StringBuilder();

        builder.Append("BEGIN:VCALENDAR").Append(LineBreak);
        builder.Append("VERSION:2.0").Append(LineBreak);
        builder.Append("PRODID:").Append(ProductId).Append(LineBreak);
        builder.Append("CALSCALE:GREGORIAN").Append(LineBreak);

        foreach (var item in events)
        {
            builder.Append("BEGIN:VEVENT").Append(LineBreak);
            AppendProperty(builder, "UID", item.Uid);
            AppendProperty(builder, "DTSTAMP", stamp);
            AppendProperty(builder, "DTSTART", FormatUtc(item.StartUtc));
            AppendProperty(builder, "DTEND", FormatUtc(item.EndUtc));
            AppendProperty(builder, "SUMMARY", Escape(item.Summary));

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                AppendProperty(builder, "DESCRIPTION", Escape(item.Description));
            }

            if (!string.IsNullOrWhiteSpace(item.Location))
            {
                AppendProperty(builder, "LOCATION", Escape(item.Location));
            }

            builder.Append("END:VEVENT").Append(LineBreak);
        }

        builder.Append("END:VCALENDAR").Append(LineBreak);
        return builder.ToString();
    }

    private static void AppendProperty(StringBuilder builder, string property, string value) =>
        builder.Append(property).Append(':').Append(value).Append(LineBreak);

    // SpecifyKind (not ToUniversalTime) — our values are already UTC wall-clock; this only labels them
    // so a DateTime read back as Unspecified is not shifted by a local-time conversion.
    private static string FormatUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc)
            .ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    /// <summary>RFC 5545 TEXT escaping: backslash, semicolon, comma and newlines.</summary>
    private static string Escape(string value) => value
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n")
        .Replace("\r", "\\n");
}
