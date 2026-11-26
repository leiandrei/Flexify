using System;
using System.ComponentModel.DataAnnotations;
using GymCrudApp.Models;

namespace GymCrudApp.ViewModels.Account
{
    public class RegisterViewModel : IValidatableObject
    {
        private static readonly DateTime MinBirthdate = new(1900, 1, 1);

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Enter a valid 11-digit Philippine mobile number (e.g., 09171234567).")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birthdate is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Birthdate")]
        public DateTime Birthdate { get; set; } = DateTime.Today.AddYears(-18);

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var today = DateTime.Today;
            var maxBirthdate = today.AddYears(-16);

            if (Birthdate.Date > today)
            {
                yield return new ValidationResult(
                    "Birthdate can’t be in the future.",
                    new[] { nameof(Birthdate) });
            }
            else if (Birthdate.Date > maxBirthdate)
            {
                yield return new ValidationResult(
                    "You must be at least 16 years old to register.",
                    new[] { nameof(Birthdate) });
            }
            else if (Birthdate.Date < MinBirthdate)
            {
                yield return new ValidationResult(
                    "Please enter a valid birthdate.",
                    new[] { nameof(Birthdate) });
            }
        }
    }
}
