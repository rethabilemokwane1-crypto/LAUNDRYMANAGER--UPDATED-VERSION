using System.Net;
using System.Net.Mail;

namespace LaundryManager.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        private SmtpClient CreateClient(out string senderEmail, out string senderName)
        {
            var server = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var portText = _config["EmailSettings:Port"] ?? "587";

            // Get the password from configuration first.
            // If it is empty/missing, use the environment variable.
            var password = _config["EmailSettings:SenderPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                password = Environment.GetEnvironmentVariable("LAUNDRY_SMTP_PASSWORD");
            }

            senderEmail = _config["EmailSettings:SenderEmail"] ?? "";
            senderName = _config["EmailSettings:SenderName"] ?? "iWS Laundry Portal";

            if (string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "SMTP email is not configured. Set EmailSettings:SenderEmail and EmailSettings:SenderPassword, or set the LAUNDRY_SMTP_PASSWORD environment variable.");
            }

            return new SmtpClient(server, int.Parse(portText))
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
        }

        public async Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string body)
        {
            using var client = CreateClient(
                out var senderEmail,
                out var senderName);

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(recipientEmail);

            await client.SendMailAsync(message);
        }

        public async Task SendEmailToManyAsync(
            List<string> recipientEmails,
            string subject,
            string body)
        {
            var uniqueRecipients = recipientEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!uniqueRecipients.Any())
                return;

            using var client = CreateClient(
                out var senderEmail,
                out var senderName);

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            foreach (var email in uniqueRecipients)
            {
                message.Bcc.Add(email);
            }

            // Gmail requires a To address.
            // Using the configured sender keeps the recipient addresses private.
            message.To.Add(senderEmail);

            await client.SendMailAsync(message);
        }
    }
}

