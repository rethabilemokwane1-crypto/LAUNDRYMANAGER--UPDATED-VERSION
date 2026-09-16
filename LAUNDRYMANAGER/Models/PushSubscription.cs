using System.ComponentModel.DataAnnotations;

namespace LaundryManager.Models
{
    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        // Which student this subscription belongs to
        [Required]
        public string StudentEmail { get; set; } = "";

        // The subscription info the browser gives us
        [Required]
        public string Endpoint { get; set; } = "";

        [Required]
        public string P256dh { get; set; } = "";

        [Required]
        public string Auth { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
