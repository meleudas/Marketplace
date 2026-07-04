namespace Marketplace.Infrastructure.External.Email;

public sealed record EmailMessageContent(string Subject, string Body);

public static class TransactionalEmailContentBuilder
{
    public static EmailMessageContent BuildConfirmation(string? frontendBaseUrl, string email, string token)
    {
        var link = EmailConfirmationLinkBuilder.Build(frontendBaseUrl, email, token);
        var body =
            "Підтвердіть email, перейшовши за посиланням:\n" + link + "\n\n" +
            "Якщо ви не реєструвались, проігноруйте цей лист.";
        return new EmailMessageContent("Підтвердження email", body);
    }

    public static EmailMessageContent BuildPasswordReset(string token)
    {
        var body =
            $"Ваш код для скидання пароля: {token}\n\n" +
            "Якщо ви не запитували скидання, змініть пароль і перевірте безпеку акаунта.";
        return new EmailMessageContent("Скидання пароля", body);
    }

    public static EmailMessageContent BuildTwoFactorCode(string code)
    {
        var body =
            $"Ваш код 2FA: {code}\n\n" +
            "Код одноразовий. Нікому його не повідомляйте.";
        return new EmailMessageContent("Код двофакторної автентифікації", body);
    }
}
