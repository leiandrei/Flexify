using System.ComponentModel.DataAnnotations;

namespace GymCrudApp.ViewModels.MembershipPlans
{
    public class MembershipPlanFormViewModel
    {
        public int PlanId { get; set; }

        [Required(ErrorMessage = "This field is required. Please provide a plan name.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Plan name must be between 3 and 100 characters long.")]
        [RegularExpression(@"^[a-zA-Z0-9\s/-]+$", ErrorMessage = "Plan name can only contain letters, numbers, spaces, slashes, and hyphens.")]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required. Please provide a plan price.")]
        [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "Price must be between 0.01 and 999,999.99.")]
        [Display(Name = "Plan Price")]
        public decimal PlanPrice { get; set; }

        [Required(ErrorMessage = "This field is required. Please provide a plan description.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters long.")]
        [Display(Name = "Description")]
        public string PlanDescription { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "This field is required. Please provide the plan duration in days.")]
        [Range(1, 3650, ErrorMessage = "Duration must be between 1 and 3,650 days.")]
        [Display(Name = "Duration (Days)")]
        public int DurationDays { get; set; } = 30;

        [Display(Name = "Active Plan")]
        public bool IsActive { get; set; } = true;
    }
}