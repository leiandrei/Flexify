using System;
using System.Collections.Generic;
using GymCrudApp.ViewModels.Members;

namespace GymCrudApp.ViewModels.Memberships
{
    public class MembershipDetailsViewModel
    {
        public int MembershipId { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string MemberContactNumber { get; set; } = string.Empty;
        public DateTime MemberJoinDate { get; set; }
        public bool MemberIsActive { get; set; } = true;

        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public int DurationDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastReminderSent { get; set; }
        public int DaysRemaining { get; set; }
        public string ComputedStatus { get; set; } = string.Empty;

        public List<MemberPaymentRowViewModel> Payments { get; set; } = new();
        public List<MemberVisitRowViewModel> Visits { get; set; } = new();
    }
}
