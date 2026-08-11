using System.Net;

namespace StudentCouncil.Application.Common.Email;

/// <summary>A renderable email: subject + HTML body.</summary>
public sealed record EmailMessage(string Subject, string HtmlBody);

/// <summary>Plain HTML templates for the Phase 2 transactional emails.</summary>
public static class EmailTemplates
{
    public static EmailMessage WelcomeWithTempPassword(string fullName, string email, string temporaryPassword)
    {
        var subject = "Достъп до Студентски съвет — временна парола";
        var body =
            $"""
            <p>Здравейте, {Enc(fullName)},</p>
            <p>Създаден е акаунт за Вас в приложението на Студентския съвет.</p>
            <p><strong>Имейл за вход:</strong> {Enc(email)}<br/>
            <strong>Временна парола:</strong> {Enc(temporaryPassword)}</p>
            <p>При първото влизане ще бъдете подканени да смените паролата си.</p>
            """;
        return new EmailMessage(subject, body);
    }

    public static EmailMessage PasswordReset(string fullName, string resetLink, int validMinutes)
    {
        var subject = "Нулиране на парола — Студентски съвет";
        var body =
            $"""
            <p>Здравейте, {Enc(fullName)},</p>
            <p>Получена е заявка за нулиране на паролата Ви. За да зададете нова парола, отворете:</p>
            <p><a href="{Enc(resetLink)}">{Enc(resetLink)}</a></p>
            <p>Линкът е валиден {validMinutes} минути. Ако не сте заявявали това, игнорирайте имейла.</p>
            """;
        return new EmailMessage(subject, body);
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
