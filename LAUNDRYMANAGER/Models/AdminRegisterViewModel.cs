using System.ComponentModel.DataAnnotations;

namespace LaundryManager.Models
{
    public class AdminRegisterViewModel
    {
        [Required(ErrorMessage = "Please enter the residence name.")]
        [StringLength(120)]
        public string ResidenceName { get; set; } = "";

        [Required(ErrorMessage = "Please enter the residence address.")]
        [StringLength(300)]
        public string ResidenceAddress { get; set; } = "";

        [Required(ErrorMessage = "Please enter a Gmail address.")]
        [EmailAddress]
        [RegularExpression(@"^[A-Za-z0-9._%+\-]+@gmail\.com$", ErrorMessage = "Residence managers must register with a Gmail address.")]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[0-9])(?=.*[!@#$%^&*(),.?""\:;{}|<>_\-+=]).*$",
            ErrorMessage = "Password must contain at least one number and one symbol.")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
