using LaundryManager.Data;
using WebPush;

namespace LaundryManager.Services
{
    public class PushNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public PushNotificationService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task SendNotificationAsync(string studentEmail, string title, string body, string url = "/")
        {
            var subscriptions = _context.PushSubscriptions
                .Where(p => p.StudentEmail == studentEmail)
                .ToList();

            if (!subscriptions.Any()) return;

            var vapidPublicKey = _config["VapidKeys:PublicKey"];
            var vapidPrivateKey = _config["VapidKeys:PrivateKey"] ?? Environment.GetEnvironmentVariable("LAUNDRY_VAPID_PRIVATE_KEY");
            var vapidSubject = _config["VapidKeys:Subject"];

            if (string.IsNullOrWhiteSpace(vapidPublicKey) || string.IsNullOrWhiteSpace(vapidPrivateKey) || string.IsNullOrWhiteSpace(vapidSubject))
                return;

            var vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
            var webPushClient = new WebPushClient();

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                title = title,
                body = body,
                url = url
            });

            foreach (var sub in subscriptions)
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                try
                {
                    await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                }
                catch (WebPushException)
                {
                    // Subscription expired or invalid — remove it so we stop trying
                    _context.PushSubscriptions.Remove(sub);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}