using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class MembershipPlans
    {
        // primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int planId { get; set; }

        // plan name (e.g., "Monthly Basic", "6-Month VIP")
        [Required(ErrorMessage = "This field is required. Please provide a plan name.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Plan name must be between 3 and 100 characters long.")]
        [RegularExpression(@"^[a-zA-Z0-9\s/-]+$", ErrorMessage = "Plan name can only contain letters, numbers, spaces, slashes, and hyphens.")]
        public string planName { get; set; } = string.Empty;

        // plan price
        [Required(ErrorMessage = "This field is required. Please provide a plan price.")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal planPrice { get; set; }

        // plan description
        [Required(ErrorMessage = "This field is required. Please provide a plan description.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters long.")]
        public string planDescription { get; set; } = string.Empty;
        
        // duration in days
        [Required(ErrorMessage = "This field is required. Please provide the plan duration in days.")]
        [Range(1, 3650, ErrorMessage = "Duration must be between 1 and 3,650 days.")]
        public int durationDays { get; set; } = 30;

        public bool isActive { get; set; } = true;
    }
}