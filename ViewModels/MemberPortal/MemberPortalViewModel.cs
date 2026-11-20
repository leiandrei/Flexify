using System;
using System.Collections.Generic;

namespace GymCrudApp.ViewModels.MemberPortal
{
    public class MemberPortalViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        // Current / Latest Subscription Summary
        public string? PlanName { get; set; }
        public string MembershipStatus { get; set; } = "No Membership"; // Active, Expiring, Expired, Inactive, No Membership
        public int DaysRemaining { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // Scoped Histories (signed-in member only)
        public List<MemberPaymentHistoryItemViewModel> Payments { get; set; } = new();
        public List<MemberAttendanceHistoryItemViewModel> AttendanceLogs { get; set; } = new();
    }

    public class MemberPaymentHistoryItemViewModel
    {
        public int PaymentId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsVoided { get; set; }
    }

    public class MemberAttendanceHistoryItemViewModel
    {
        public int AttendanceId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        public string DurationDisplay
        {
            get
            {
                if (!CheckOut.HasValue) return "In Gym";
                var span = CheckOut.Value - CheckIn;
                return $"{(int)span.TotalHours}h {span.Minutes}m";
            }
        }
    }
}