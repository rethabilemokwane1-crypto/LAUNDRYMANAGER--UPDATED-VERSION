using LaundryManager.Data;
using LaundryManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace LaundryManager.Controllers
{
    public class PushController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PushController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class SubscribeRequest
        {
            public string StudentEmail { get; set; } = "";
            public string Endpoint { get; set; } = "";
            public string P256dh { get; set; } = "";
            public string Auth { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            // Avoid duplicate subscriptions for the same student + endpoint
            var existing = _context.PushSubscriptions
                .FirstOrDefault(p => p.StudentEmail == request.StudentEmail && p.Endpoint == request.Endpoint);

            if (existing == null)
            {
                var subscription = new PushSubscription
                {
                    StudentEmail = request.StudentEmail,
                    Endpoint = request.Endpoint,
                    P256dh = request.P256dh,
                    Auth = request.Auth
                };
                _context.PushSubscriptions.Add(subscription);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}