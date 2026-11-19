// members tbl: member_id, user_id, full_name, phone, birthdate, email, join_date, is_active

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class Members
    {
        // primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int memberId { get; set; }

        // foreign key linking to Users table
        [Required(ErrorMessage = "This field is required. Please select a user account.")]
        public int userId { get; set; }

        [ForeignKey("userId")]
        public Users? User { get; set; }

        // full name
        [Required(ErrorMessage = "This field is required. Please provide the member's full name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters long.")]
        [RegularExpression(@"^[a-zA-Z\s.-]+$", ErrorMessage = "Full name can only contain letters, spaces, dots, and hyphens.")]
        public string fullName { get; set; } = string.Empty;

        // phone / contact number (PH mobile format)
        [Required(ErrorMessage = "This field is required. Please provide a contact number.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [RegularExpression(@"^(\+63|0)9\d{9}$", ErrorMessage = "Enter a valid mobile number (e.g., 09171234567 or +639171234567).")]
        public string contactNumber { get; set; } = string.Empty;

        // birthdate (listed in your table comment)
        [Required(ErrorMessage = "This field is required. Please provide a birthdate.")]
        [DataType(DataType.Date)]
        public DateTime birthdate { get; set; }

        // email (reusing your custom GmailOnly attribute)
        [Required(ErrorMessage = "This field is required. Please provide a valid email.")]
        [EmailAddress(ErrorMessage = "Invalid email address. Please provide a valid email.")]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "The field email must be between 10 and 100 characters long.")]
        [GmailOnly(ErrorMessage = "Only @gmail.com email addresses are allowed.")]
        public string userEmail { get; set; } = string.Empty;

        // join date
        [Required(ErrorMessage = "This field is required. Please provide a join date.")]
        [DataType(DataType.Date)]
        public DateTime joinDate { get; set; } = DateTime.Today;

        public bool isActive { get; set; } = true;
    }
}