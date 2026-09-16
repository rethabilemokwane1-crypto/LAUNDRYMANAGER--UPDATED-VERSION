using System.ComponentModel.DataAnnotations;

namespace LaundryManager.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress]
        [RegularExpression(@"^(?:[0-9]{9}@mywsu\.ac\.za|[A-Za-z0-9._%+\-]+@gmail\.com)$",
            ErrorMessage = "Use your WSU student email or registered Gmail admin email.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
