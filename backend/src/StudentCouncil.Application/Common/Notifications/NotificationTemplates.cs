using System.Globalization;

namespace StudentCouncil.Application.Common.Notifications;

/// <summary>A renderable notification: a short title and a plain-text body (push + in-app center).</summary>
public sealed record NotificationContent(string Title, string Body);

/// <summary>
/// Bulgarian notification copy per <see cref="NotificationType"/>, mirroring the <c>EmailTemplates</c>
/// model. Plain text — no HTML escaping is needed for push/in-app bodies.
/// </summary>
public static class NotificationTemplates
{
    public static NotificationContent TaskAssigned(string taskTitle) =>
        new("Нова задача", $"Назначена Ви е задача: {taskTitle}");

    public static NotificationContent TaskDueSoon(string taskTitle) =>
        new("Наближаващ срок", $"Срокът на задача „{taskTitle}“ изтича до 24 часа.");

    public static NotificationContent TaskOverdue(string taskTitle) =>
        new("Просрочена задача", $"Срокът на задача „{taskTitle}“ изтече.");

    public static NotificationContent TaskStatusChanged(string taskTitle, TaskStatus status) =>
        new("Промяна на статус", $"Задача „{taskTitle}“ е със статус „{StatusLabel(status)}“.");

    public static NotificationContent TaskComment(string authorName, string taskTitle) =>
        new("Нов коментар", $"{authorName} коментира по задача „{taskTitle}“.");

    public static NotificationContent EventCreated(string eventTitle) =>
        new("Ново събитие", $"Добавено е събитие: {eventTitle}");

    public static NotificationContent EventChanged(string eventTitle) =>
        new("Променено събитие", $"Събитие „{eventTitle}“ беше обновено.");

    public static NotificationContent EventCancelled(string eventTitle) =>
        new("Отменено събитие", $"Събитие „{eventTitle}“ беше отменено.");

    public static NotificationContent EventReminder(string eventTitle, EventReminderLead lead) =>
        new("Напомняне за събитие",
            $"Събитие „{eventTitle}“ започва {(lead == EventReminderLead.OneHour ? "след 1 час" : "след 24 часа")}.");

    public static NotificationContent DutyReminder(int year, int month) =>
        new("Напомняне за дежурство",
            $"Все още не сте изпълнили нормата за дежурства за {MonthName(month)} {year}.");

    private static string StatusLabel(TaskStatus status) => status switch
    {
        TaskStatus.New => "Нова",
        TaskStatus.InProgress => "В процес",
        TaskStatus.InReview => "За проверка",
        TaskStatus.Completed => "Завършена",
        TaskStatus.Cancelled => "Отменена",
        _ => status.ToString()
    };

    private static string MonthName(int month) =>
        month is >= 1 and <= 12
            ? CultureInfo.GetCultureInfo("bg-BG").DateTimeFormat.GetMonthName(month)
            : month.ToString(CultureInfo.InvariantCulture);
}

/// <summary>How far ahead an event reminder fires (spec 9: 24h and 1h before the start).</summary>
public enum EventReminderLead
{
    TwentyFourHours,
    OneHour
}
