using System.ComponentModel.DataAnnotations;

namespace LaundryManager.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your WSU email address.")]
        [RegularExpression(@"^[0-9]{9}@mywsu\.ac\.za$", ErrorMessage = "Student email must be exactly 9 digits followed by @mywsu.ac.za")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter a password.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[0-9])(?=.*[!@#$%^&*(),.?""\:;{}|<>_\-+=]).*$",
            ErrorMessage = "Password must contain at least one number and one symbol.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Please select your residence.")]
        public int? ResidenceId { get; set; }
        public string? FullName { get; set; }
    }
}
