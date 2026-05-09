using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace WebApplication4.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        // Конструктор, который "оживляет" логгер
        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailMessage = new MimeMessage();

            // Твоя почта отправителя
            emailMessage.From.Add(new MailboxAddress("DevPortfolio", "valeriayurtsevich2008@gmail.com"));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlMessage };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 465, true);

                    // ПАРОЛЬ БЕЗ ПРОБЕЛОВ
                    await client.AuthenticateAsync("valeriayurtsevich2008@gmail.com", "hvduxjznfcppblcc");

                    await client.SendAsync(emailMessage);
                    _logger.LogInformation("Письмо успешно отправлено на {Email}", email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке почты");
                    throw;
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}