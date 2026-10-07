using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LaundryManager.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _http = new HttpClient();
        }

        private string GetApiKey()
        {
            var key = Environment.GetEnvironmentVariable("BREVO_API_KEY");

            if (string.IsNullOrWhiteSpace(key))
                key = _config["EmailSettings:BrevoApiKey"];

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    "Brevo API key is not configured. Set the BREVO_API_KEY environment variable.");

            return key;
        }

        private string GetSenderEmail()
        {
            return _config["EmailSettings:SenderEmail"]
                   ?? "rethabilemokwane1@gmail.com";
        }

        private string GetSenderName()
        {
            return _config["EmailSettings:SenderName"] ?? "iWS Laundry Portal";
        }

        public async Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string body)
        {
            await SendViaBrevoAsync(
                new List<string> { recipientEmail },
                subject,
                body);
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

            await SendViaBrevoAsync(uniqueRecipients, subject, body);
        }

        private async Task SendViaBrevoAsync(
            List<string> recipients,
            string subject,
            string body)
        {
            var apiKey = GetApiKey();
            var senderEmail = GetSenderEmail();
            var senderName = GetSenderName();

            var payload = new
            {
                sender = new
                {
                    name = senderName,
                    email = senderEmail
                },
                to = recipients.Select(r => new { email = r }).ToArray(),
                subject = subject,
                textContent = body
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email")
            {
                Content = content
            };

            request.Headers.Add("api-key", apiKey);
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Brevo API failed ({response.StatusCode}): {errorBody}");
            }
        }
    }
}