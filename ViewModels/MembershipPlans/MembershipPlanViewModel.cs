using System.ComponentModel.DataAnnotations;

namespace GymCrudApp.ViewModels.MembershipPlans
{
    public class MembershipPlanViewModel
    {
        public int PlanId { get; set; }

        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal PlanPrice { get; set; }

        [Display(Name = "Description")]
        public string PlanDescription { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}