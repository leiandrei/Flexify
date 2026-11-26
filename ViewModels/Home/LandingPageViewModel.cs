using System.Collections.Generic;

namespace GymCrudApp.ViewModels.Home
{
    public class LandingPageViewModel
    {
        public string HeroTitle { get; set; } = "Show up. Train hard. Stay ready.";
        public string HeroSubtitle { get; set; } = "A simple plan for people who just want to work out. Always know your days left from your phone.";

        public List<PublicMembershipPlanViewModel> Plans { get; set; } = new();

        public string FacilityName { get; set; } = "Flexify Fitness Hub";
        public string FacilityAddress { get; set; } = "2nd Floor, Fitness Plaza, Metro Manila";
        public string OperatingHours { get; set; } = "Mon–Sat: 6:00 AM – 10:00 PM | Sun: 8:00 AM – 6:00 PM";
        public string ContactPhone { get; set; } = "+63 917 555 0199";
        public string ContactEmail { get; set; } = "support@flexifygym.local";
    }

    public class PublicMembershipPlanViewModel
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public int DurationDays { get; set; }
        public string PlanDescription { get; set; } = string.Empty;
    }
}
