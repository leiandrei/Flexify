using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class Staff
    {
        // primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StaffID { get; set; }

        // foreign key linking to Users table
        [Required(ErrorMessage = "This field is required. Please select a user account.")]
        public int userId { get; set; }

        [ForeignKey("userId")]
        public Users? User { get; set; }

        // full name
        [Required(ErrorMessage = "This field is required. Please provide the staff member's full name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters long.")]
        [RegularExpression(@"^[a-zA-Z\s.-]+$", ErrorMessage = "Full name can only contain letters, spaces, dots, and hyphens.")]
        public string fullName { get; set; } = string.Empty;

        // job position
        [Required(ErrorMessage = "This field is required. Please specify the staff position.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 50 characters long.")]
        [RegularExpression(@"^[a-zA-Z\s/-]+$", ErrorMessage = "Position can only contain letters, spaces, slashes, and hyphens.")]
        public string position { get; set; } = string.Empty;

        // date of birth
        [Required(ErrorMessage = "This field is required. Please provide a date of birth.")]
        [DataType(DataType.Date)]
        public DateTime dateOfBirth { get; set; }
    }
}