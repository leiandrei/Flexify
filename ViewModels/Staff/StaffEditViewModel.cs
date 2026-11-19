using System;
using System.ComponentModel.DataAnnotations;

namespace GymCrudApp.ViewModels.Staff
{
    public class StaffEditViewModel
    {
        public int StaffID { get; set; }

        [Required(ErrorMessage = "This field is required. Please provide the staff member's full name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters long.")]
        [RegularExpression(@"^[a-zA-Z\s.-]+$", ErrorMessage = "Full name can only contain letters, spaces, dots, and hyphens.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required. Please specify the staff position.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 50 characters long.")]
        [RegularExpression(@"^[a-zA-Z\s/-]+$", ErrorMessage = "Position can only contain letters, spaces, slashes, and hyphens.")]
        [Display(Name = "Position")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required. Please provide a date of birth.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Active Account")]
        public bool IsActive { get; set; } = true;
    }
}