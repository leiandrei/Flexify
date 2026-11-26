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

        public int? CurrentPlanId { get; set; }
        public string? PlanName { get; set; }
        public decimal? PlanPrice { get; set; }
        public int? PlanDurationDays { get; set; }
        public string MembershipStatus { get; set; } = "No Membership";
        public int DaysRemaining { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int VisitsThisMonth { get; set; }

        public List<MemberPaymentHistoryItemViewModel> Payments { get; set; } = new();
        public List<MemberAttendanceHistoryItemViewModel> AttendanceLogs { get; set; } = new();
        public List<MemberPortalPlanItemViewModel> AvailablePlans { get; set; } = new();
    }

    public class MemberPortalPlanItemViewModel
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public int DurationDays { get; set; }
        public string PlanDescription { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
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
                if (!CheckOut.HasValue) return "—";
                var span = CheckOut.Value - CheckIn;
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            }
        }
    }
}
