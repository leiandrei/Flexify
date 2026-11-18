using System;

namespace GymCrudApp.ViewModels.Attendance
{
    public class MemberCheckInSearchResultViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? PlanName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string MembershipStatus { get; set; } = "No Membership"; // Active, Expiring, Expired, Inactive, No Membership
        public bool HasOpenCheckInToday { get; set; }

        public bool CanCheckIn =>
            !HasOpenCheckInToday &&
            (MembershipStatus == "Active" || MembershipStatus == "Expiring");
    }
}