using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using CO2Crawler.Models;

namespace CO2Crawler.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendReportAsync(string filePath)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(_settings.Recipient));
            message.Subject = "CO2Crawler-rapport";

            var builder = new BodyBuilder
            {
                TextBody = "Vedhæftet CO2-rapport som CSV."
            };
            builder.Attachments.Add(filePath);

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine($"📧 E-mail sendt til: {_settings.Recipient}");
        }
    }
}
