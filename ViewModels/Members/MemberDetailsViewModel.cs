using System;
using System.Collections.Generic;

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

        public int? CurrentPlanId { get; set; }
        public string? CurrentPlanName { get; set; }
        public decimal? CurrentPlanPrice { get; set; }
        public int? PlanDurationDays { get; set; }
        public DateTime? MembershipStartDate { get; set; }
        public DateTime? MembershipEndDate { get; set; }
        public int? DaysRemaining { get; set; }
        public string MembershipStatus { get; set; } = "No Membership";

        public List<MemberPaymentRowViewModel> Payments { get; set; } = new();
        public List<MemberVisitRowViewModel> Visits { get; set; } = new();
    }

    public class MemberPaymentRowViewModel
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string RecordedByStaffName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsVoided { get; set; }
    }

    public class MemberVisitRowViewModel
    {
        public DateTime CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        public string TimeSpentLabel
        {
            get
            {
                if (!CheckOut.HasValue) return "—";
                var span = CheckOut.Value - CheckIn;
                if (span.TotalMinutes < 1) return "<1m";
                var hours = (int)span.TotalHours;
                var mins = span.Minutes;
                if (hours <= 0) return $"{mins}m";
                return $"{hours}h {mins:00}m";
            }
        }
    }
}
