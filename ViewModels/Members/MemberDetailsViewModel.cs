using System;

namespace GymCrudApp.ViewModels.Members
{
    public class MemberDetailsViewModel
    {
        public int MemberId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime Birthdate { get; set; }
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }
        public string? CurrentPlanName { get; set; }
        public DateTime? MembershipEndDate { get; set; }
        public int? DaysRemaining { get; set; }
        public string MembershipStatus { get; set; } = "No Membership";
    }
}