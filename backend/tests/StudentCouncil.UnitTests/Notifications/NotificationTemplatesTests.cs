using FluentAssertions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Enums;
using TaskStatus = StudentCouncil.Domain.Enums.TaskStatus;

namespace StudentCouncil.UnitTests.Notifications;

public class NotificationTemplatesTests
{
    public static IEnumerable<object[]> AllContents()
    {
        yield return [NotificationTemplates.TaskAssigned("Задача")];
        yield return [NotificationTemplates.TaskDueSoon("Задача")];
        yield return [NotificationTemplates.TaskOverdue("Задача")];
        yield return [NotificationTemplates.TaskStatusChanged("Задача", TaskStatus.InReview)];
        yield return [NotificationTemplates.TaskComment("Иван", "Задача")];
        yield return [NotificationTemplates.EventCreated("Събитие")];
        yield return [NotificationTemplates.EventChanged("Събитие")];
        yield return [NotificationTemplates.EventCancelled("Събитие")];
        yield return [NotificationTemplates.EventReminder("Събитие", EventReminderLead.TwentyFourHours)];
        yield return [NotificationTemplates.EventReminder("Събитие", EventReminderLead.OneHour)];
        yield return [NotificationTemplates.DutyReminder(2026, 6)];
    }

    [Theory]
    [MemberData(nameof(AllContents))]
    public void Every_template_has_a_non_empty_title_and_body(NotificationContent content)
    {
        content.Title.Should().NotBeNullOrWhiteSpace();
        content.Body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Task_templates_embed_the_subject()
    {
        NotificationTemplates.TaskAssigned("Плакати").Body.Should().Contain("Плакати");
        NotificationTemplates.TaskComment("Мария", "Плакати").Body.Should().Contain("Мария").And.Contain("Плакати");
    }

    [Fact]
    public void Event_reminder_distinguishes_the_lead_time()
    {
        NotificationTemplates.EventReminder("Среща", EventReminderLead.OneHour).Body.Should().Contain("1 час");
        NotificationTemplates.EventReminder("Среща", EventReminderLead.TwentyFourHours).Body.Should().Contain("24 часа");
    }

    [Fact]
    public void Duty_reminder_names_the_month_in_bulgarian()
    {
        NotificationTemplates.DutyReminder(2026, 6).Body.Should().Contain("юни");
    }
}
