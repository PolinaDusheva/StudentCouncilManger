using FluentAssertions;
using StudentCouncil.Application.Common.Calendar;

namespace StudentCouncil.UnitTests.Calendar;

public class IcsBuilderTests
{
    private static DateTime Utc(int y, int m, int d, int h, int min) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_emits_a_well_formed_vcalendar_with_utc_times()
    {
        var ics = new IcsEvent(
            "evt-1@studentcouncil.ue-varna.bg",
            Utc(2026, 7, 1, 9, 0),
            Utc(2026, 7, 1, 10, 30),
            "Meeting",
            "Agenda",
            "Room 101");

        var output = IcsBuilder.Build([ics], Utc(2026, 6, 27, 12, 0));

        output.Should().StartWith("BEGIN:VCALENDAR\r\n");
        output.Should().Contain("VERSION:2.0");
        output.Should().Contain("PRODID:-//Student Council UE-Varna//Calendar//EN");
        output.Should().Contain("BEGIN:VEVENT").And.Contain("END:VEVENT");
        output.Should().EndWith("END:VCALENDAR\r\n");

        output.Should().Contain("UID:evt-1@studentcouncil.ue-varna.bg");
        output.Should().Contain("DTSTAMP:20260627T120000Z");
        output.Should().Contain("DTSTART:20260701T090000Z");
        output.Should().Contain("DTEND:20260701T103000Z");
        output.Should().Contain("SUMMARY:Meeting");
        output.Should().Contain("DESCRIPTION:Agenda");
        output.Should().Contain("LOCATION:Room 101");
    }

    [Fact]
    public void Build_escapes_special_characters_in_text_fields()
    {
        var ics = new IcsEvent(
            "evt-2@studentcouncil.ue-varna.bg",
            Utc(2026, 7, 1, 9, 0),
            Utc(2026, 7, 1, 10, 0),
            "Sales; growth, plan",
            "Line one\nLine two",
            null);

        var output = IcsBuilder.Build([ics], Utc(2026, 6, 27, 12, 0));

        output.Should().Contain("SUMMARY:Sales\\; growth\\, plan");
        output.Should().Contain("DESCRIPTION:Line one\\nLine two");
    }

    [Fact]
    public void Build_omits_optional_fields_when_absent()
    {
        var ics = new IcsEvent(
            "evt-3@studentcouncil.ue-varna.bg",
            Utc(2026, 7, 1, 9, 0),
            Utc(2026, 7, 1, 10, 0),
            "Bare event",
            null,
            null);

        var output = IcsBuilder.Build([ics], Utc(2026, 6, 27, 12, 0));

        output.Should().NotContain("DESCRIPTION:");
        output.Should().NotContain("LOCATION:");
    }

    [Fact]
    public void Build_writes_one_vevent_per_entry()
    {
        var events = new[]
        {
            new IcsEvent("a@x", Utc(2026, 7, 1, 9, 0), Utc(2026, 7, 1, 10, 0), "A", null, null),
            new IcsEvent("b@x", Utc(2026, 7, 2, 9, 0), Utc(2026, 7, 2, 10, 0), "B", null, null)
        };

        var output = IcsBuilder.Build(events, Utc(2026, 6, 27, 12, 0));

        System.Text.RegularExpressions.Regex.Matches(output, "BEGIN:VEVENT").Should().HaveCount(2);
    }
}
