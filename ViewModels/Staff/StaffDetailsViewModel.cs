using System;
using System.Collections.Generic;

namespace GymCrudApp.ViewModels.Staff
{
    public class StaffDetailsViewModel
    {
        public int StaffID { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; }

        public List<StaffPaymentRowViewModel> Payments { get; set; } = new();
    }

    public class StaffPaymentRowViewModel
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsVoided { get; set; }
    }
}
