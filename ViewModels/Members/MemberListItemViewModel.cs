using System;
namespace GymCrudApp.ViewModels.Members
{
    public class MemberListItemViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }

        // Latest membership (from seeded Memberships + Plans) — same fields Details already uses
        public string? PlanName { get; set; }
        public DateTime? MembershipEndDate { get; set; }
        public int? DaysRemaining { get; set; }
        public string MembershipStatus { get; set; } = "No plan";
    }
}