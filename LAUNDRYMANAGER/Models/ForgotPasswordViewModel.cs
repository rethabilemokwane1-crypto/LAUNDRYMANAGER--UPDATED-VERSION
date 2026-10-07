using System.ComponentModel.DataAnnotations;

namespace LaundryManager.Models
{
    public class ForgotPasswordViewModel
    {
        // Accepts BOTH account types on one page:
        //   students -> 9 digits @mywsu.ac.za   e.g. 221098765@mywsu.ac.za
        //   managers -> any @gmail.com          e.g. manager@gmail.com
        // The two halves are joined with | (OR) inside a single group.
        [Required(ErrorMessage = "Please enter your email address.")]
        [RegularExpression(
            @"^(?:[0-9]{9}@mywsu\.ac\.za|[A-Za-z0-9._%+\-]+@gmail\.com)$",
            ErrorMessage = "Use your WSU student email (9 digits @mywsu.ac.za) or your manager Gmail address.")]
        public string Email { get; set; } = "";
    }
}
