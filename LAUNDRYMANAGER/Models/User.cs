using System;

namespace LaundryManager.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        public string Role { get; set; } = "Student";
        public int? ResidenceId { get; set; }
        public Residence? Residence { get; set; }
    }
}
