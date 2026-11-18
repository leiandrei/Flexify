using System;

namespace GymCrudApp.ViewModels.Payments
{
    public class PaymentDetailsViewModel
    {
        public int PaymentId { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public int MembershipId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime MembershipStartDate { get; set; }
        public DateTime MembershipEndDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public string RecordedByStaffName { get; set; } = string.Empty;
        public bool IsVoided { get; set; }
    }
}