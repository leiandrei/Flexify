using System;

namespace GymCrudApp.ViewModels.Memberships
{
    public class MembershipListItemViewModel
    {
        public int MembershipId { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public string ComputedStatus { get; set; } = string.Empty;
    }
}